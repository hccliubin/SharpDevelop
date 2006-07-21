//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a tool.
//     Runtime Version: 1.1.4322.2032
//
//     Changes to this file may cause incorrect behavior and will be lost if 
//     the code is regenerated.
// </autogenerated>
//------------------------------------------------------------------------------


using System;
using System.Drawing;	
using System.ComponentModel;
using System.Xml.Serialization;
	
/// <summary>
/// This Class is the BaseClass for <see cref="BaseTextItem"></see>
/// and <see cref="BaseGraphicItem"></see>
/// </summary>
namespace SharpReportCore {
	public class BaseReportItem : SharpReportCore.BaseReportObject,
											IItemRenderer{
		
		private int xOffset;
		private bool drawBorder;	
		private Color foreColor;
		
		private Font font;
		
		public event EventHandler<BeforePrintEventArgs> ItemPrinting;
		public event EventHandler<AfterPrintEventArgs> ItemPrinted;
		
		public event EventHandler Disposed;
		
		public BaseReportItem() :base(){
			
		}
		
		
		#region EventHandling
		
		protected void NotiyfyAfterPrint (PointF afterPrintLocation) {
//			System.Console.WriteLine("\tNotiyfyAfterPrint");
			if (this.ItemPrinted != null) {
				AfterPrintEventArgs rea = new AfterPrintEventArgs (afterPrintLocation);
				ItemPrinted(this, rea);
			}
		}
		
		private void NotifyBeforePrint () {
//			System.Console.WriteLine("\tNotifyBeforePrint");
			if (this.ItemPrinting != null) {
				BeforePrintEventArgs ea = new BeforePrintEventArgs ();
				ItemPrinting (this,ea);
			}
		}
		
		#endregion
		
		#region overrides
		public override void Render(ReportPageEventArgs rpea){
			base.Render(rpea);
			this.NotifyBeforePrint();
		}
		
		#endregion
		
		#region virtual method's
		protected RectangleF DrawingRectangle (SizeF measureSize) {	
			PointF upperLeft = new PointF (this.Location.X ,
			                             this.Location.Y + this.SectionOffset);
			SizeF lowerRight = new SizeF(0,0);
			
			if ((this.CanGrow == true )||(this.CanShrink == true)){
				if (measureSize.Height > this.Size.Height ) {
					lowerRight = new SizeF (this.Size.Width,
					                         measureSize.Height);
				                   
				}
			} else {
				lowerRight = new SizeF (this.Size.Width,
					                    this.Size.Height);
			}
			return new RectangleF (upperLeft,lowerRight);		                       
		}
		
		#endregion
		
		#region Properties
		
		[XmlIgnoreAttribute]
		[Browsable(false)]
		public int XOffset {
			get {
				return xOffset;
			}
			set {
				xOffset = value;
			}
		}
		
		
		[Browsable(true),
		 Category("Appearance"),
		 Description("Draw a Border around the Item")]
		public bool DrawBorder {
			get {
				return drawBorder;
			}
			set {
				drawBorder = value;
				base.NotifyPropertyChanged ("DrawBorder");
			}
		}
		
		[Category("Appearance")]
		public virtual Color ForeColor {
			get {
				return foreColor;
			}
			set {
				foreColor = value;
				base.NotifyPropertyChanged ("ForeColor");
			}
		}
		
		[Category("Appearance")]
		public virtual Font Font {
			get {
				return this.font;
			}
			set {

				this.font = value;
				NotifyPropertyChanged ("Font");
			}
		}
			
		#endregion
		
		#region IDisposeable
		public override void Dispose () {
			Dispose(true);
            GC.SuppressFinalize(this);
		}
		
		~BaseReportItem(){
			Dispose(false);
		}
		
		protected override void Dispose(bool disposing) {
			try {
				if (disposing){
					if (this.font != null){
						this.font = null;
						this.font.Dispose();
					}
				}
			} finally {
				if (this.Disposed != null) {
					this.Disposed (this,EventArgs.Empty);
				}
				base.Dispose(disposing);
			}
		}

		#endregion
		
	}
}
