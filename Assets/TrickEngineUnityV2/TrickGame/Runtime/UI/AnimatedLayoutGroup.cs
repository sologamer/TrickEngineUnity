using System;
using System.Collections.Generic;
using System.Linq;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedLayoutGroup : MonoBehaviour
{
    public AnimatedLayoutGroupSettings Settings;

    private LayoutGroup _layoutGroup;
    
    public List<AnimatedLayoutLink> ParentLinks = new();
    
    private AnimatedLayoutGroup _cloned;
    private AnimatedLayoutGroup _parent;
    private ContentSizeFitter _fitter;
    private float _timePassed;
    private int _iteration;
    private Transform _tr;

    public bool IsParent => _cloned != null;

    private void Awake()
    {
        _tr = transform;
        _layoutGroup = GetComponent<LayoutGroup>();
        _fitter = GetComponent<ContentSizeFitter>();
        // don't recursive for childs
        if (_parent != null) return;

        Run();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (IsParent)
        {
            _cloned.Sync();
        }
    }

    private void Sync()
    {
        if (_parent != null)
        {
            var parentTransform = _parent.transform;
            List<(Transform child, AnimatedLayoutLink link)> parentChilds = new(parentTransform.childCount);
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                var child = parentTransform.GetChild(i);
                parentChilds.Add((child, child.GetComponent<AnimatedLayoutLink>()));
            }

            var clonedTransform = transform;
            var clonedChildCount = clonedTransform.childCount;
            List<(Transform child, AnimatedLayoutLink link)> clonedChilds = new(clonedChildCount);
            for (int i = 0; i < clonedChildCount; i++)
            {
                var child = clonedTransform.GetChild(i);
                clonedChilds.Add((child, child.GetComponent<AnimatedLayoutLink>()));
            }

            for (var index = 0; index < parentChilds.Count; index++)
            {
                var (child, link) = parentChilds[index];
                if (link == null)
                {
                    // check if one is free
                    var validLinkIndex = clonedChilds.FindIndex(tuple => tuple.link == null);
                    if (validLinkIndex != -1)
                    {
                        // we need to create a link
                        link = child.gameObject.AddComponent<AnimatedLayoutLink>();
                        link.SetChild(this, clonedChilds[validLinkIndex].child.gameObject.AddComponent<AnimatedLayoutLink>().SetParent(this, link));
                        clonedChilds.RemoveAt(validLinkIndex);
                    }
                    else
                    {
                        // we need to create a cloned child

                        // clone the child and clear it's inner component
                        var clonedChild = Instantiate(child.gameObject, _tr);
                        clonedChild.GetComponentsInChildren<MonoBehaviour>(true)
                            .Where(component =>
                                component is not (LayoutGroup or LayoutElement or ContentSizeFitter
                                    or AnimatedLayoutGroup)).ToList().ForEach(Destroy);
                        
                        // link it
                        link = child.gameObject.AddComponent<AnimatedLayoutLink>();
                        link.SetChild(this, clonedChild.AddComponent<AnimatedLayoutLink>().SetParent(this, link));
                    }
                    
                    _parent.ParentLinks.Add(link);
                }
            }
        }
    }

    private void Run()
    {
        if (_layoutGroup == null) return;
        
        if (Settings.ChildWatchUpdatePerSecond == 0) Settings.ChildWatchUpdatePerSecond = 1;
            
        ParentLinks.Clear();
        
        enabled = false;
        gameObject.SetActive(false);
        _cloned = Instantiate(this, _tr.parent);
        _cloned.name = "[LinkedClone] " + _cloned.name;
        _cloned.hideFlags = HideFlags.DontSave;
        _cloned._parent = this;
        _cloned.Settings = Settings;
        _cloned.GetComponentsInChildren<MonoBehaviour>(true)
            .Where(component => component is not (LayoutGroup or LayoutElement or ContentSizeFitter or AnimatedLayoutGroup)).ToList().ForEach(Destroy);
        gameObject.SetActive(true);
        _cloned.Sync();
        enabled = true;

        if (_fitter.enabled || _layoutGroup.enabled)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutGroup.transform as RectTransform);
        }
            
        _fitter.enabled = false;
        _layoutGroup.enabled = false;

        _cloned.gameObject.SetActive(true);
        _cloned._fitter.enabled = true;
        _cloned._layoutGroup.enabled = true;
    }

    private void Update()
    {
        if (_timePassed >= 1.0f / Settings.ChildWatchUpdatePerSecond)
        {
            _timePassed = 0;

            if (Settings.DuplicateLinkCheckerEveryIteration > 0 && _iteration % Settings.DuplicateLinkCheckerEveryIteration == 0)
            {
                // destroy the component, it will be synced later
                var childLinks = _tr.GetComponentsInChildren<AnimatedLayoutLink>();
                childLinks.Where(link => link.LinkTarget == null).ToList().ForEach(Destroy);
                var unlink = childLinks
                    .Where(link => link.LinkTarget != null)
                    .GroupBy(link => link.LinkTarget)
                    .SelectMany(links => links.Skip(1))
                    .ToList();
                unlink.ForEach(Destroy);
            }

            for (var index = ParentLinks.Count - 1; index >= 0; index--)
            {
                AnimatedLayoutLink link = ParentLinks[index];
                link.Check();
            }

            if(_cloned == null) Run();
            _cloned.Sync();

            _iteration++;
        }
        _timePassed += Time.deltaTime;
    }

    public void RemoveLayoutLink(AnimatedLayoutLink linkA, AnimatedLayoutLink linkB)
    {
        _parent.ParentLinks.Remove(linkA);
        if (linkA != null) Destroy(linkA.gameObject);
        if (linkB != null) Destroy(linkB.gameObject);
    }
}

[Serializable]
public class AnimatedLayoutGroupSettings
{
    /// <summary>
    /// The amount of updates to watch our childs
    /// </summary>
    public int ChildWatchUpdatePerSecond = 10;
    
    /// <summary>
    /// The amount of iterations before we do a duplicate link check (expensive)
    /// </summary>
    public int DuplicateLinkCheckerEveryIteration = 10;
    
    /// <summary>
    /// The tween settings
    /// </summary>
    public TweenSettings AnimationTweenSettings;
    
    /// <summary>
    /// Watch for child index changes
    /// </summary>
    public bool WatchForChildIndex = true;
    
    /// <summary>
    /// Animate position
    /// </summary>
    public bool AnimatePosition = true;
    
    /// <summary>
    /// Animate size delta
    /// </summary>
    public bool AnimateSizeDelta = false;
}

public class AnimatedLayoutLink : MonoBehaviour
{
    public AnimatedLayoutLink LinkTarget;
    private Routine _sizeDeltaRoutine;
    private Routine _anchorPositionRoutine;

    public AnimatedLayoutGroup Root { get; set; }
    public RectTransform LinkRectTransform { get; set; }
    public RectTransform MyRectTransform { get; set; }
    public bool IsChild { get; set; }

    public AnimatedLayoutLink SetChild(AnimatedLayoutGroup animatedLayoutGroup, AnimatedLayoutLink link)
    {
        Root = animatedLayoutGroup;
        LinkTarget = link;
        LinkRectTransform = link.transform as RectTransform;   
        MyRectTransform = transform as RectTransform;   
        IsChild = false;
        return this;
    }


    public AnimatedLayoutLink SetParent(AnimatedLayoutGroup animatedLayoutGroup, AnimatedLayoutLink link)
    {
        Root = animatedLayoutGroup;
        LinkTarget = link;
        LinkRectTransform = link.transform as RectTransform;   
        MyRectTransform = transform as RectTransform;   
        IsChild = true;
        return this;
    }


    public void Check()
    {
        if (Root == null) return;
        if (MyRectTransform == null || LinkRectTransform == null)
        {
            Root.RemoveLayoutLink(this, LinkTarget);
            return;
        }
        if (Root.Settings.WatchForChildIndex)
        {
            int linkTargetIndex = MyRectTransform.GetSiblingIndex();
            if (LinkRectTransform.GetSiblingIndex() != linkTargetIndex)
            {
                LinkRectTransform.SetSiblingIndex(linkTargetIndex);
            }
        }
        
        if (MyRectTransform.sizeDelta != LinkRectTransform.sizeDelta)
        {
            if (Root.Settings.AnimateSizeDelta)
            {
                if (!_sizeDeltaRoutine.Exists())
                {
                    _sizeDeltaRoutine = MyRectTransform
                        .SizeDeltaTo(LinkRectTransform.sizeDelta, Root.Settings.AnimationTweenSettings).Play();
                }
            }
            else
            {
                MyRectTransform.sizeDelta = LinkRectTransform.sizeDelta;
            }
        }

        if (MyRectTransform.anchoredPosition != LinkRectTransform.anchoredPosition)
        {
            if (Root.Settings.AnimatePosition)
            {
                if (!_anchorPositionRoutine.Exists())
                    _anchorPositionRoutine = MyRectTransform
                        .AnchorPosTo(LinkRectTransform.anchoredPosition, Root.Settings.AnimationTweenSettings).Play();
            }
            else
            {
                MyRectTransform.anchoredPosition = LinkRectTransform.anchoredPosition;
            }
        }
    }
}