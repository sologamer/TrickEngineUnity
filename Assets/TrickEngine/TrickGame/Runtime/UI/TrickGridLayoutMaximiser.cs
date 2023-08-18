using System;
using UnityEngine;
using UnityEngine.UI;

namespace TrickCore
{
    /// <summary>
    /// GridLayoutGroup helper function to maximize the grid size
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    [ExecuteAlways]
    public sealed class TrickGridLayoutMaximiser : MonoBehaviour
    {
        [Tooltip("Override the number of columns to aim for (or zero for default/disabled). " +
                 "Takes priority over rows.")]
        [SerializeField]
        private int numColumnsOverride = 0;

        [Tooltip("Override the number of rows to aim for (or zero for default/disabled).")]
        [SerializeField]
        private int numRowsOverride = 0;

        private bool isSettingSizes = false; // A flag to prevent recursive invocation

        private void OnEnable()
        {
            SetSizes();
        }

        private void OnRectTransformDimensionsChange()
        {
            SetSizes();
        }

        [ContextMenu("Set sizes")]
        private void SetSizes()
        {
            // Check for recursion
            if (isSettingSizes)
            {
                return;
            }
            isSettingSizes = true;

            var gridLayoutGroup = GetComponent<GridLayoutGroup>();

            int columns = this.numColumnsOverride;
            int rows = this.numRowsOverride;

            switch (gridLayoutGroup.constraint)
            {
                case GridLayoutGroup.Constraint.Flexible:
                    // Do nothing
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

            float width = gridLayoutGroup.cellSize.x; // Default to existing width
            float height = gridLayoutGroup.cellSize.y; // Default to existing height

            if (columns > 0)
            {
                width = (size.x - (columns - 1) * spacing.x) / columns;
            }

            if (rows > 0)
            {
                height = (size.y - (rows - 1) * spacing.y) / rows;
            }

            gridLayoutGroup.cellSize = new Vector2(width, height);

            isSettingSizes = false;
        }

    }
}
