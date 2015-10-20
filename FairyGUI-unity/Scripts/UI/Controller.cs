using System.Collections.Generic;
using FairyGUI.Utils;
using System;

namespace FairyGUI
{
	public class Controller : EventDispatcher
	{
		public string name;
		public EventListener onChanged { get; private set; }

		internal GComponent parent;

		int _selectedIndex;
		int _previousIndex;
		List<string> _pageIds;
		List<string> _pageNames;
		List<PageTransition> _pageTransitions;
		Transition _playingTransition;

		static uint _nextPageId;

		public Controller()
		{
			_pageIds = new List<string>();
			_pageNames = new List<string>();
			_selectedIndex = -1;
			_previousIndex = -1;

			onChanged = new EventListener(this, "onChanged");
		}

		public int selectedIndex
		{
			get
			{
				return _selectedIndex;
			}
			set
			{
				if (_selectedIndex != value)
				{
					if (value > _pageIds.Count - 1)
						throw new IndexOutOfRangeException("" + value);

					_previousIndex = _selectedIndex;
					_selectedIndex = value;
					parent.ApplyController(this);

					onChanged.Call();

					if (_playingTransition != null)
					{
						_playingTransition.Stop();
						_playingTransition = null;
					}

					if (_pageTransitions != null)
					{
						foreach (PageTransition pt in _pageTransitions)
						{
							if (pt.toIndex == _selectedIndex && (pt.fromIndex == -1 || pt.fromIndex == _previousIndex))
							{
								_playingTransition = parent.GetTransition(pt.transitionName);
								break;
							}
						}

						if (_playingTransition != null)
							_playingTransition.Play(() => { _playingTransition = null; });
					}
				}
			}
		}

		//功能和设置selectedIndex一样，但不会触发事件
		public void SetSelectedIndex(int value)
		{
			if (_selectedIndex != value)
			{
				if (value > _pageIds.Count - 1)
					throw new IndexOutOfRangeException("" + value);

				_previousIndex = _selectedIndex;
				_selectedIndex = value;
				parent.ApplyController(this);

				if (_playingTransition != null)
				{
					_playingTransition.Stop();
					_playingTransition = null;
				}
			}
		}

		//功能和设置selectedPage一样，但不会触发事件
		public void SetSelectedPage(string value)
		{
			int i = _pageNames.IndexOf(value);
			if (i == -1)
				i = 0;
			this.SetSelectedIndex(i);
		}

		public int previsousIndex
		{
			get { return _previousIndex; }
		}

		public string selectedPage
		{
			get
			{
				if (_selectedIndex == -1)
					return null;
				else
					return _pageNames[_selectedIndex];
			}
			set
			{
				int i = _pageNames.IndexOf(value);
				if (i == -1)
					i = 0;
				this.selectedIndex = i;
			}
		}

		public string previousPage
		{
			get
			{
				if (_previousIndex == -1)
					return null;
				else
					return _pageNames[_previousIndex];
			}
		}

		public int pageCount
		{
			get { return _pageIds.Count; }
		}

		public string GetPageName(int index)
		{
			return _pageNames[index];
		}

		public void AddPage(string name)
		{
			if (name == null)
				name = string.Empty;

			AddPageAt(name, _pageIds.Count);
		}

		public void AddPageAt(string name, int index)
		{
			string nid = "_" + (_nextPageId++);
			if (index == _pageIds.Count)
			{
				_pageIds.Add(nid);
				_pageNames.Add(name);
			}
			else
			{
				_pageIds.Insert(index, nid);
				_pageNames.Insert(index, name);
			}
		}

		public void RemovePage(string name)
		{
			int i = _pageNames.IndexOf(name);
			if (i != -1)
			{
				_pageIds.RemoveAt(i);
				_pageNames.RemoveAt(i);
				if (_selectedIndex >= _pageIds.Count)
					this.selectedIndex = _selectedIndex - 1;
				else
					parent.ApplyController(this);
			}
		}

		public void RemovePageAt(int index)
		{
			_pageIds.RemoveAt(index);
			_pageNames.RemoveAt(index);
			if (_selectedIndex >= _pageIds.Count)
				this.selectedIndex = _selectedIndex - 1;
			else
				parent.ApplyController(this);
		}

		public void ClearPages()
		{
			_pageIds.Clear();
			_pageNames.Clear();
			if (_selectedIndex != -1)
				this.selectedIndex = -1;
			else
				parent.ApplyController(this);
		}

		internal int GetPageIndexById(string aId)
		{
			return _pageIds.IndexOf(aId);
		}

		public string GetPageIdByName(string aName)
		{
			return _pageIds[_pageNames.IndexOf(aName)];
		}

		internal string GetPageNameById(string aId)
		{
			return _pageNames[_pageIds.IndexOf(aId)];
		}

		internal string GetPageId(int index)
		{
			return _pageIds[index];
		}

		internal string selectedPageId
		{
			get
			{
				if (_selectedIndex == -1)
					return null;
				else
					return _pageIds[_selectedIndex];
			}
			set
			{
				int i = _pageIds.IndexOf(value);
				this.selectedIndex = i;
			}
		}

		internal string oppositePageId
		{
			set
			{
				int i = _pageIds.IndexOf(value);
				if (i > 0)
					this.selectedIndex = 0;
				else if (_pageIds.Count > 1)
					this.selectedIndex = 1;
			}
		}

		internal string previousPageId
		{
			get
			{
				if (_previousIndex == -1)
					return null;
				else
					return _pageIds[_previousIndex];
			}
		}

		public void Setup(XML xml)
		{
			string[] arr;

			name = xml.GetAttribute("name");
			arr = xml.GetAttributeArray("pages");
			if (arr != null)
			{
				int cnt = arr.Length;
				for (int i = 0; i < cnt; i += 2)
				{
					_pageIds.Add(arr[i]);
					_pageNames.Add(arr[i + 1]);
				}
			}

			arr = xml.GetAttributeArray("transitions");
			if (arr != null)
			{
				_pageTransitions = new List<PageTransition>();

				int cnt = arr.Length;
				for (int i = 0; i < cnt; i++)
				{
					string str = arr[i];

					PageTransition pt = new PageTransition();
					int k = str.IndexOf("=");
					pt.transitionName = str.Substring(k + 1);
					str = str.Substring(0, k);
					k = str.IndexOf("-");
					pt.toIndex = int.Parse(str.Substring(k + 1));
					str = str.Substring(0, k);
					if (str == "*")
						pt.fromIndex = -1;
					else
						pt.fromIndex = int.Parse(str);
					_pageTransitions.Add(pt);
				}
			}

			if (parent != null && _pageIds.Count >= 0)
				_selectedIndex = 0;
			else
				_selectedIndex = -1;
		}
	}

	class PageTransition
	{
		public string transitionName;
		public int fromIndex;
		public int toIndex;
	}
}
