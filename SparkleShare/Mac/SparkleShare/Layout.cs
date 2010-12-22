// 
// Layout.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Drawing;
using MonoMac.AppKit;
using System.Linq;
namespace MonoDevelop.Platform.Mac
{
	interface ILayout
	{
		LayoutRequest BeginLayout ();
		void EndLayout (LayoutRequest request, PointF origin, SizeF allocation);
	}
	
	class LayoutRequest
	{
		public SizeF Size { get; set; }
		public bool Visible { get; set; }
		public bool ExpandWidth { get; set; }
		public bool ExpandHeight { get; set; }
	}
	
	abstract class LayoutBox : IEnumerable<ILayout>, ILayout
	{
		List<ILayout> children = new List<ILayout> ();
		
		public float Spacing { get; set; }
		public float PadLeft { get; set; }
		public float PadRight { get; set; }
		public float PadTop { get; set; }
		public float PadBottom { get; set; }
		public LayoutAlign Align { get; set; }
		
		public LayoutDirection Direction { get; set; }
		
		public LayoutBox (LayoutDirection direction, float spacing) : this (direction, spacing, 0)
		{
		}
		
		public LayoutBox (LayoutDirection direction, float spacing, float padding)
		{
			PadLeft = PadRight = PadTop = PadBottom = padding;
			this.Direction = direction;
			this.Spacing = spacing;
			this.Align = LayoutAlign.Center;
		}
		
		public int Count { get { return children.Count; } }
		
		bool IsHorizontal { get { return Direction == LayoutDirection.Horizontal; } }
		
		public IEnumerator<ILayout> GetEnumerator ()
		{
			return children.GetEnumerator ();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return children.GetEnumerator ();
		}
		
		public void Add (ILayout child)
		{
			children.Add (child);
			OnChildAdded (child);
		}
		
		ContainerLayoutRequest request = new ContainerLayoutRequest ();
		
		public virtual LayoutRequest BeginLayout ()
		{
			float width = 0;
			float height = 0;
			
			request.ChildRequests.Clear ();
			request.ChildRequests.AddRange (children.Select (c => c.BeginLayout ()));
			
			foreach (var r in request.ChildRequests) {
				if (!r.Visible)
					continue;
				request.Visible = true;
				if (r.ExpandWidth)
					request.ExpandWidth = true;
				if (r.ExpandHeight)
					request.ExpandHeight = true;
				
				if (IsHorizontal) {
					if (width != 0)
						width += Spacing;
					width += r.Size.Width;
					height = Math.Max (height, r.Size.Height);
				} else {
					if (height != 0)
						height += Spacing;
					height += r.Size.Height;
					width = Math.Max (width, r.Size.Width);
				}
			}
			
			request.Size = new SizeF (width + PadLeft + PadRight, height + PadTop + PadBottom);
			return request;
		}
		
		public virtual void EndLayout (LayoutRequest request, PointF origin, SizeF allocation)
		{	
			var childRequests =  ((ContainerLayoutRequest) request).ChildRequests;
			
			allocation = new SizeF (allocation.Width - PadLeft - PadRight, allocation.Height - PadBottom - PadTop);
			origin = new PointF (origin.X + PadLeft, origin.Y + PadBottom);
			
			var size = request.Size;
			size.Height -= (PadTop + PadBottom);
			size.Width -= (PadLeft + PadRight);
			
			int wExpandCount = 0;
			int hExpandCount = 0;
			int visibleCount = 0;
			foreach (var childRequest in childRequests) {
				if (childRequest.Visible)
					visibleCount++;
				else
					continue;
				if (childRequest.ExpandWidth)
					wExpandCount++;
				if (childRequest.ExpandHeight)
					hExpandCount++;
			}
			
			float wExpand = 0;
			if (allocation.Width > size.Width) {
				wExpand = allocation.Width - size.Width;
				if (wExpandCount > 0)
					wExpand /= wExpandCount;
			}
			float hExpand = 0;
			if (allocation.Height > size.Height) {
				hExpand = allocation.Height - size.Height;
				if (hExpandCount > 0)
					hExpand /= hExpandCount;
			}
			
			if (Direction == LayoutDirection.Horizontal) {
				float pos = PadLeft;
				if (wExpandCount == 0) {
					if (Align == LayoutAlign.End)
						pos += wExpand;
					else if (Align == LayoutAlign.Center)
						pos += wExpand / 2;
				}
				for (int i = 0; i < childRequests.Count; i++) {
					var child = children[i];
					var childReq = childRequests[i];
					if (!childReq.Visible)
						continue;
					
					var childSize = new SizeF (childReq.Size.Width, allocation.Height);
					if (childReq.ExpandWidth) {
						childSize.Width += wExpand;
					} else if (hExpandCount == 0 && Align == LayoutAlign.Fill) {
						childSize.Width += wExpand / visibleCount;
					}
					
					child.EndLayout (childReq, new PointF (pos, origin.Y), childSize);
					pos += childSize.Width + Spacing;
				}
			} else {
				float pos = PadBottom;
				if (hExpandCount == 0) {
					if (Align == LayoutAlign.End)
						pos += hExpand;
					else if (Align == LayoutAlign.Center)
						pos += hExpand / 2;
				}
				for (int i = 0; i < childRequests.Count; i++) {
					var child = children[i];
					var childReq = childRequests[i];
					if (!childReq.Visible)
						continue;
					
					var childSize = new SizeF (allocation.Width, childReq.Size.Height);
					if (childReq.ExpandHeight) {
						childSize.Height += hExpand;
					} else if (hExpandCount == 0 && Align == LayoutAlign.Fill) {
						childSize.Height += hExpand / visibleCount;
					}
					
					child.EndLayout (childReq, new PointF (origin.X, pos), childSize);
					pos += childSize.Height + Spacing;
				}
			}
		}
		
		protected abstract void OnChildAdded (ILayout child);
		
		class ContainerLayoutRequest : LayoutRequest
		{
			public List<LayoutRequest> ChildRequests = new List<LayoutRequest> ();
		}
	}
	
	public enum LayoutAlign
	{
		Begin, Center, End, Fill
	}
	
	public enum LayoutDirection
	{
		Horizontal, Vertical
	}
	
	abstract class LayoutAlignment : ILayout
	{
		public LayoutAlignment ()
		{
			XAlign = YAlign = LayoutAlign.Center;
		}
		
		public LayoutAlign XAlign { get; set; }
		public LayoutAlign YAlign { get; set; }
		public bool  ExpandHeight { get; set; }
		public bool  ExpandWidth { get; set; }
		public float MinHeight { get; set; }
		public float MinWidth { get; set; }
		public float PadLeft { get; set; }
		public float PadRight { get; set; }
		public float PadTop { get; set; }
		public float PadBottom { get; set; }
		public bool Visible { get; set; }
		
		LayoutRequest request = new LayoutRequest ();
		
		public virtual LayoutRequest BeginLayout ()
		{
			request.Size = new SizeF (MinWidth + PadLeft + PadRight, MinHeight + PadTop + PadBottom);
			request.ExpandHeight = this.ExpandHeight;
			request.ExpandWidth = this.ExpandWidth;
			request.Visible = this.Visible;
			return request;
		}
		
		public virtual void EndLayout (LayoutRequest request, PointF origin, SizeF allocation)
		{
			var frame = new RectangleF (origin.X + PadLeft, origin.Y + PadBottom, 
				allocation.Width - PadLeft - PadRight, allocation.Height - PadTop - PadBottom);
			
			if (allocation.Height > request.Size.Height) {
				if (YAlign != LayoutAlign.Fill) {
					frame.Height = request.Size.Height - PadTop - PadBottom;
					if (YAlign == LayoutAlign.Center) {
						frame.Y += (allocation.Height - request.Size.Height) / 2;
					} else if (YAlign == LayoutAlign.End) {
						frame.Y += (allocation.Height - request.Size.Height);
					}
				}
			}
			
			if (allocation.Width > request.Size.Width) {
				if (XAlign != LayoutAlign.Fill) {
					frame.Width = request.Size.Width - PadLeft - PadRight;
					if (XAlign == LayoutAlign.Center) {
						frame.X += (allocation.Width - request.Size.Width) / 2;
					} else if (XAlign == LayoutAlign.End) {
						frame.X += (allocation.Width - request.Size.Width);
					}
				}
			}
			
			OnLayoutEnded (frame);
		}
		
		protected abstract void OnLayoutEnded (RectangleF frame);
	}
}