using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace LabelVal.ImageRolls.Services
{
    public class SelectionService
    {
        private readonly List<WeakReference<ListView>> _listViews = new List<WeakReference<ListView>>();
        private bool _isNotifying;

        public void RegisterListView(ListView listView)
        {
            _listViews.Add(new WeakReference<ListView>(listView));
        }

        public void UnregisterListView(ListView listView)
        {
            for (int i = _listViews.Count - 1; i >= 0; i--)
            {
                if (_listViews[i].TryGetTarget(out var target) && target == listView)
                {
                    _listViews.RemoveAt(i);
                    break;
                }
            }
        }

        public void NotifySelectionChanged(ListView source)
        {
            if (_isNotifying) return;

            _isNotifying = true;
            try
            {
                foreach (var listViewRef in _listViews)
                {
                    if (listViewRef.TryGetTarget(out var listView) && listView != source)
                    {
                        listView.SelectedItem = null;
                    }
                }
            }
            finally
            {
                _isNotifying = false;
            }
        }
    }
}
