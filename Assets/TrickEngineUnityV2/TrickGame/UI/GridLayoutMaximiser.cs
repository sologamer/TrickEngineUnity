using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
[ExecuteAlways]
public sealed class GridLayoutMaximiser : MonoBehaviour
{

    [Tooltip("Override the number of columns to aim for (or zero for default/disabled).\n" +
             "Takes priority over rows.")]
    [SerializeField]
    private int numColumnsOverride = 0;

    [Tooltip("Override the number of rows to aim for (or zero for default/disabled).")] [SerializeField]
    private int numRowsOverride = 0;

    public void OnEnable()
    {
        setSizes();
    }

    private void OnRectTransformDimensionsChange()
    {
        setSizes();
    }

    [ContextMenu("Set sizes")]
    private void setSizes()
    {
        var gridLayoutGroup = GetComponent<GridLayoutGroup>();
        var columns = this.numColumnsOverride;
        var rows = this.numRowsOverride;
        switch (gridLayoutGroup.constraint)
        {
            case GridLayoutGroup.Constraint.Flexible: // nop
                break;

            case GridLayoutGroup.Constraint.FixedColumnCount:
                columns = gridLayoutGroup.constraintCount;
                break;

            case GridLayoutGroup.Constraint.FixedRowCount:
                rows = gridLayoutGroup.constraintCount;
                break;

            default:
                throw new ArgumentOutOfRangeException(gridLayoutGroup.constraint.ToString());
        }

        var padding = gridLayoutGroup.padding;
        var spacing = gridLayoutGroup.spacing;
        var size = ((RectTransform)transform).rect.size - new Vector2(padding.horizontal, padding.vertical);
        float width, height;
        if (0 < columns)
        {
            width = (size.x - (columns - 1) * spacing.x) / columns;
            if (0 < rows)
            {
                height = (size.y - (rows - 1) * spacing.y) / rows;
            }
            else
            {
                // TODO: account for different vertical to horizontal spacing
                height = width;
            }
        }
        else
        {
            if (0 < rows)
            {
                // rows specified but not columns
                // TODO: account for different vertical to horizontal spacing
                width = height = (size.y - (rows - 1) * spacing.y) / rows;
            }
            else
            {
                // neither specified
                return;
            }
        }

        gridLayoutGroup.cellSize = new Vector2(width, height);
    }

}