using System;
using System.Collections.Generic;

namespace FairyGUI
{
	public class GObjectPool
	{
		public delegate void InitCallbackDelegate(GObject obj);
		public InitCallbackDelegate initCallback;

		Dictionary<string, Queue<GObject>> _pool;

		public GObjectPool()
		{
			_pool = new Dictionary<string, Queue<GObject>>();
		}

		public void Clear()
		{
			foreach (KeyValuePair<string, Queue<GObject>> kv in _pool)
			{
				Queue<GObject> list = kv.Value;
				foreach (GObject obj in list)
					obj.Dispose();
			}
			_pool.Clear();
		}

		public int count
		{
			get { return _pool.Count; }
		}

		public GObject GetObject(string url)
		{
			Queue<GObject> arr;
			if (!_pool.TryGetValue(url, out arr))
			{
				arr = new Queue<GObject>();
				_pool.Add(url, arr);
			}

			if (arr.Count > 0)
			{
				return arr.Dequeue();
			}

			GObject obj = UIPackage.CreateObjectFromURL(url);
			if (obj == null)
				throw new Exception(url + " not exists");

			if (initCallback != null)
				initCallback(obj);

			return obj;
		}

		public void ReturnObject(GObject obj)
		{
			string url = obj.resourceURL;
			Queue<GObject> arr;
			if (_pool.TryGetValue(url, out arr))
				arr.Enqueue(obj);
		}
	}
}
