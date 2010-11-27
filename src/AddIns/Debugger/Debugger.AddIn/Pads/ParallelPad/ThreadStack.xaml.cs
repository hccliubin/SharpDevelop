﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.Core.Presentation;
using ICSharpCode.SharpDevelop.Debugging;
using ICSharpCode.SharpDevelop.Gui.Pads;
using ICSharpCode.SharpDevelop.Services;

namespace Debugger.AddIn.Pads.ParallelPad
{
	public partial class ThreadStack : UserControl
	{
		public static SolidColorBrush SelectedBrush = new SolidColorBrush(Color.FromRgb(84, 169, 255));
		
		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register("IsSelected", typeof(bool), typeof(ThreadStack),
			                            new FrameworkPropertyMetadata());
		
		public event EventHandler FrameSelected;
		
		private ObservableCollection<ExpandoObject> itemCollection = new ObservableCollection<ExpandoObject>();
		
		private ToolTip toolTip = new ToolTip();
		private List<uint> threadIds = new List<uint>();
		
		public ThreadStack()
		{
			InitializeComponent();
			datagrid.ToolTip = toolTip;
			datagrid.ToolTipOpening += OnToolTipOpening;
			datagrid.PreviewMouseMove += new MouseEventHandler(datagrid_PreviewMouseMove);
			datagrid.MouseLeave += delegate { toolTip.IsOpen = false; };
		}
		
		#region Public Properties
		
		public Process Process { get; set; }
		
		public int Level { get; set; }
		
		public bool IsSelected {
			get { return (bool)GetValue(IsSelectedProperty); }
			set {
				if (value) {
					BorderParent.BorderBrush = SelectedBrush;
					BorderParent.BorderThickness = new Thickness(5);
				}
				else {
					BorderParent.BorderBrush = Brushes.Black;
					BorderParent.BorderThickness = new Thickness(3);
				}
				
				SetValue(IsSelectedProperty, value);
				
				SelectParent(value);
			}
		}
		
		public ThreadStack ThreadStackParent { get; set; }
		
		public List<ThreadStack> ThreadStackChildren { get; set; }
		
		public List<uint> ThreadIds {
			get {
				return threadIds;
			}
		}
		
		public ObservableCollection<ExpandoObject> ItemCollection {
			get {
				return itemCollection;
			}
			
			set {
				itemCollection = value;
				this.datagrid.ItemsSource = itemCollection;
			}
		}
		
		#endregion
		
		#region Public Methods
		
		public void UpdateThreadIds(params uint[] threadIds)
		{
			this.threadIds.AddRange(threadIds);
			
			if (this.threadIds.Count > 1)
				this.HeaderText.Text = this.threadIds.Count.ToString() + " Threads";
			else
				this.HeaderText.Text = "1 Thread";
		}
		
		public void ClearImages()
		{
			foreach(dynamic item in itemCollection) {
				item.Image = null;
			}
		}
		
		#endregion
		
		#region Private Methods
		
		private void SelectParent(bool isSelected)
		{
			var ts = this.ThreadStackParent;
			while(ts != null) {
				ts.IsSelected = isSelected;
				ts = ts.ThreadStackParent;
			}
		}
		
		void datagrid_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			var result = VisualTreeHelper.HitTest(this, e.GetPosition(this));
			if (result != null)
			{
				var row = TryFindParent<DataGridRow>(result.VisualHit);
				if (row != null)
				{
					datagrid.SelectedItem = row.DataContext;
					if (toolTip.IsOpen)
						toolTip.IsOpen = false;
					toolTip.IsOpen = true;
					e.Handled = true;
				}
			}
		}
		
		private void Datagrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (Process.IsRunning) return;
			
			dynamic selectedItem = datagrid.SelectedItem;
			if (selectedItem != null) {
				if (ThreadIds.Count > 1) {
					datagrid.ContextMenu = CreateContextMenu(selectedItem);
					datagrid.ContextMenu.IsOpen = true;
				}
				else
				{
					SelectFrame(ThreadIds[0], selectedItem);
				}
			}
		}
		
		private void SelectFrame(uint threadId, ExpandoObject selectedItem)
		{
			if (selectedItem == null)
				return;
			
			var thread = Process.Threads.Find(t => t.ID == threadId);
			if (thread == null)
				return;

			if (FrameSelected != null)
				FrameSelected(this, EventArgs.Empty);
			
			this.IsSelected = true;
			
			dynamic obj = selectedItem;
			Process.SelectedThread = thread;
			foreach(var frame in thread.Callstack)
			{
				if (frame.GetMethodName() == obj.MethodName)
				{
					Process.SelectedThread.SelectedStackFrame = frame;
					obj.Image = PresentationResourceService.GetImage("Bookmarks.CurrentLine").Source;
					((WindowsDebugger)DebuggerService.CurrentDebugger).JumpToCurrentLine();
					break;
				}
			}
		}
		
		private void Datagrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (Process.IsRunning) return;
			
			dynamic selectedItem = datagrid.SelectedItem;
			if (selectedItem == null)
				return;
			
			datagrid.ContextMenu = CreateContextMenu(selectedItem);
			datagrid.ContextMenu.IsOpen = true;
		}
		
		private ContextMenu CreateContextMenu(ExpandoObject item)
		{
			dynamic obj = item;
			
			var menu = new ContextMenu();
			foreach (var id in ThreadIds)
			{
				MenuItem m = new MenuItem();
				m.IsCheckable = true;
				m.IsChecked = id == Process.SelectedThread.ID;
				m.Click += delegate(object sender, RoutedEventArgs e) {
					var menuItem = e.OriginalSource as MenuItem;
					SelectFrame((uint)menuItem.Tag, item);
				};
				m.Tag = id;
				m.Header = id.ToString() + ":" + obj.MethodName;
				
				menu.Items.Add(m);
			}
			
			return menu;
		}
		
		private void OnToolTipOpening(object sender, ToolTipEventArgs e)
		{
			StackPanel panel = new StackPanel();
			
			dynamic selectedItem = datagrid.SelectedItem;
			if (selectedItem == null) {
				panel.Children.Add(new TextBlock { Text = "No item selected" });
				this.toolTip.Content = panel;
				return;
			}
			
			foreach(var thread in Process.Threads)
			{
				if (ThreadIds.Contains(thread.ID))
				{
					foreach (var frame in thread.Callstack)
					{
						if (selectedItem.MethodName == frame.GetMethodName())
						{
							TextBlock tb = new TextBlock();
							tb.Text = thread.ID + ": " + CallStackPadContent.GetFullName(frame);
							panel.Children.Add(tb);
						}
					}
				}
			}
			
			this.toolTip.Content = panel;
		}
		
		#endregion
		
		#region Static Methods
		
		private static T TryFindParent<T>(DependencyObject child) where T : DependencyObject
		{
			if (child is T) return child as T;

			DependencyObject parentObject = GetParentObject(child);
			if (parentObject == null) return null;

			var parent = parentObject as T;
			if (parent != null && parent is T)
			{
				return parent;
			}
			else
			{
				return TryFindParent<T>(parentObject);
			}
		}

		private static DependencyObject GetParentObject(DependencyObject child)
		{
			if (child == null) return null;

			ContentElement contentElement = child as ContentElement;
			if (contentElement != null)
			{
				DependencyObject parent = ContentOperations.GetParent(contentElement);
				if (parent != null) return parent;

				FrameworkContentElement fce = contentElement as FrameworkContentElement;
				return fce != null ? fce.Parent : null;
			}

			FrameworkElement frameworkElement = child as FrameworkElement;
			if (frameworkElement != null)
			{
				DependencyObject parent = frameworkElement.Parent;
				if (parent != null) return parent;
			}

			return VisualTreeHelper.GetParent(child);
		}
		
		#endregion
	}
}