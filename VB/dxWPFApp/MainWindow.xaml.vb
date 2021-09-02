Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Documents
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Navigation
Imports System.Windows.Shapes
Imports DevExpress.Xpf.Core
Imports DevExpress.Xpf.Printing
Imports DevExpress.XtraReports.UI
Imports DevExpress.DataAccess.Sql
Imports DevExpress.DataAccess.ConnectionParameters
Imports DevExpress.DataAccess.Sql.DataApi
Imports DevExpress.XtraReports.Parameters
Imports DevExpress.XtraPrinting
Imports DevExpress.Xpf.Printing.PreviewControl.Native.Models
Imports System.Reflection

Namespace dxWPFApp
	''' <summary>
	''' Interaction logic for MainWindow.xaml
	''' </summary>
	Partial Public Class MainWindow
		Inherits DXWindow

		Public Sub New()
			InitializeComponent()
		End Sub
		Private masterReport As XtraReport
		Private detailReport As XtraReport
		Private currentPageIndex As Integer
		Private Sub Button_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			Dim w As New Window()
			Dim preview As New DocumentPreviewControl()
			AddHandler preview.DocumentPreviewMouseClick, AddressOf preview_DocumentPreviewMouseClick
			w.Content = preview
			masterReport = GetMasterReport()
			preview.DocumentSource = masterReport
			masterReport.CreateDocument()
			w.Show()
		End Sub

		Private Sub preview_DocumentPreviewMouseClick(ByVal d As DependencyObject, ByVal e As DocumentPreviewMouseEventArgs)
			Dim preview As DocumentPreviewControl = TryCast(e.OriginalSource, DocumentPreviewControl)
			Dim model As ReportDocumentViewModel = TryCast(preview.Document, ReportDocumentViewModel)
			Dim pi As PropertyInfo = model.GetType().GetProperty("Report", BindingFlags.NonPublic Or BindingFlags.Public Or BindingFlags.Instance Or BindingFlags.Static)

			Dim currentReport As XtraReport = TryCast(pi.GetValue(model, Nothing), XtraReport)
			If currentReport Is Nothing Then
			Return
			End If
			If String.Equals(currentReport.Tag.ToString(), "Master") Then
				If e.ElementTag IsNot Nothing AndAlso Not String.IsNullOrEmpty(e.ElementTag.ToString()) Then
					Dim categoryName As String = (TryCast(e.Brick, VisualBrick)).Text
					Dim categoryID As Integer = Convert.ToInt32(e.ElementTag)
					currentPageIndex = e.PageIndex + 1
					Dim detailReport As XtraReport = GetDetailReport(categoryName, categoryID)
					preview.DocumentSource = detailReport
				End If
			End If
			If String.Equals(currentReport.Tag.ToString(), "Detail") Then
				preview.DocumentSource = Nothing
				preview.DocumentSource = masterReport
				masterReport.CreateDocument()
				preview.CurrentPageNumber = currentPageIndex
			End If
		End Sub
		Private Function GetMasterReport() As XtraReport
			If masterReport Is Nothing Then
				CreateMasterReport()
			End If
			Return masterReport
		End Function
		#Region "*****Master Report Initialization*****"
		Private Sub CreateMasterReport()
			Dim ds As SqlDataSource = GetMasterData()
			masterReport = New XtraReport()
			masterReport.Tag = "Master"
			masterReport.DataSource = ds
			masterReport.DataMember = "Categories"
			CreateMasterReportControls(masterReport)
		End Sub
		Private Sub CreateMasterReportControls(ByVal report As XtraReport)
			Dim tbl As New XRTable()
			tbl.BeginInit()
			Dim row As New XRTableRow()
			row.HeightF = 40
			Dim ds As SqlDataSource = TryCast(report.DataSource, SqlDataSource)
			Dim dataMember As String = report.DataMember
			Dim categoriesTable As ITable = TryCast(ds.Result(dataMember), ITable)
			For Each col As IColumn In categoriesTable.Columns
				If col.Type Is GetType(Byte()) Then
					Continue For
				End If
				Dim cell As New XRTableCell()
				cell.DataBindings.Add(New XRBinding("Text", Nothing, String.Format("{0}.{1}", dataMember, col.Name)))
				If col.Name = "CategoryName" Then
					cell.Font = New System.Drawing.Font(cell.Font.FontFamily, 14F, System.Drawing.FontStyle.Underline Or System.Drawing.FontStyle.Bold)
					cell.DataBindings.Add(New XRBinding("Tag", Nothing, String.Format("{0}.{1}", dataMember, "CategoryID")))
					cell.ForeColor = System.Drawing.Color.Blue
					cell.NavigateUrl = " "
				End If
				row.Cells.Add(cell)
			Next col
			tbl.Rows.Add(row)
			AddHandler tbl.BeforePrint, AddressOf tbl_BeforePrint
			tbl.AdjustSize()
			tbl.EndInit()
			Dim detailBand As DetailBand = Nothing
			If report.Bands(BandKind.Detail) Is Nothing Then
				report.Bands.Add(New DetailBand())
			End If
			detailBand = TryCast(report.Bands(BandKind.Detail), DetailBand)
			detailBand.HeightF = tbl.HeightF + 2
			detailBand.PageBreak = PageBreak.AfterBand
			detailBand.Controls.Add(tbl)
		End Sub
		Private Sub tbl_BeforePrint(ByVal sender As Object, ByVal e As System.Drawing.Printing.PrintEventArgs)
			Dim table As XRTable = (DirectCast(sender, XRTable))
			table.LocationF = New DevExpress.Utils.PointFloat(0F, 0F)

			table.WidthF = (TryCast((TryCast(sender, XRTable)).Report, XtraReport)).PageWidth - (TryCast((TryCast(sender, XRTable)).Report, XtraReport)).Margins.Left - (TryCast((TryCast(sender, XRTable)).Report, XtraReport)).Margins.Right
		End Sub
		Private Shared Function GetMasterData() As SqlDataSource
			Dim ds As New SqlDataSource(New Access2007ConnectionParameters("..\..\App_Data\nwind.mdb", ""))
			ds.Queries.Add(New CustomSqlQuery("Categories", "select * from Categories"))
			ds.RebuildResultSchema()
			Return ds
		End Function
		#End Region
		#Region "*****Detail Report Initialization*****"
		Private Function GetDetailReport(ByVal categoryName As String, ByVal categoryID As Integer) As XtraReport
			If detailReport Is Nothing Then
				CreateDetailReport()
			End If
			detailReport.Parameters("catIDParam").Value = categoryID
			detailReport.Parameters("catNameParam").Value = categoryName
			detailReport.CreateDocument()
			Return detailReport
		End Function
		Private Sub CreateDetailReport()
			detailReport = New XtraReport()
			detailReport.Tag = "Detail"
			detailReport.DataSource = GetDetailData()
			detailReport.DataMember = "Products"
			CreateDetailReportParameters(detailReport)
			detailReport.FilterString = String.Format("[CategoryID] = ?catIDParam")
			CreateDetailReportControls(detailReport)
		End Sub
		Private Sub CreateDetailReportParameters(ByVal detailReport As XtraReport)
			Dim categoryNameParameter As Parameter = New DevExpress.XtraReports.Parameters.Parameter()
			categoryNameParameter.Visible = False
			categoryNameParameter.Name = "catNameParam"
			categoryNameParameter.Type = GetType(String)
			detailReport.Parameters.Add(categoryNameParameter)
			Dim categoryIDParameter As Parameter = New DevExpress.XtraReports.Parameters.Parameter()
			categoryIDParameter.Visible = False
			categoryIDParameter.Name = "catIDParam"
			categoryIDParameter.Type = GetType(Integer)
			detailReport.Parameters.Add(categoryIDParameter)
		End Sub
		Private Sub CreateDetailReportControls(ByVal report As XtraReport)
			Dim titleLbl As New XRLabel() With {
				.LocationF = New System.Drawing.PointF(0, 0),
				.SizeF = New System.Drawing.SizeF(600, 30),
				.Font = New System.Drawing.Font("Arial", 18F, System.Drawing.FontStyle.Bold Or System.Drawing.FontStyle.Italic),
				.ForeColor = System.Drawing.Color.Red
			}
			titleLbl.DataBindings.Add(New XRBinding(report.Parameters("catNameParam"), "Text", "Details for {0} category"))
			Dim reportHeaderBand As ReportHeaderBand = Nothing
			If report.Bands(BandKind.ReportHeader) Is Nothing Then
				report.Bands.Add(New ReportHeaderBand())
			End If
			reportHeaderBand = TryCast(report.Bands(BandKind.ReportHeader), ReportHeaderBand)
			reportHeaderBand.HeightF = titleLbl.HeightF + 10
			reportHeaderBand.Controls.Add(titleLbl)

			Dim homeLabel As New XRLabel() With {
				.LocationF = New System.Drawing.PointF(0, 0),
				.SizeF = New System.Drawing.SizeF(400, 30),
				.Text = "Back to categories",
				.Font = New System.Drawing.Font("Arial", 16F, System.Drawing.FontStyle.Bold Or System.Drawing.FontStyle.Italic Or System.Drawing.FontStyle.Underline),
				.NavigateUrl = " ",
				.ForeColor = System.Drawing.Color.Blue
			}
			Dim pageHeaderBand As PageHeaderBand = Nothing
			If report.Bands(BandKind.PageHeader) Is Nothing Then
				report.Bands.Add(New PageHeaderBand())
			End If
			pageHeaderBand = TryCast(report.Bands(BandKind.PageHeader), PageHeaderBand)
			pageHeaderBand.HeightF = titleLbl.HeightF
			pageHeaderBand.Controls.Add(homeLabel)

			Dim tbl As New XRTable()
			tbl.BeginInit()
			tbl.Borders = DevExpress.XtraPrinting.BorderSide.All
			Dim row As New XRTableRow()
			row.HeightF = 40
			Dim ds As SqlDataSource = TryCast(report.DataSource, SqlDataSource)
			Dim dataMember As String = report.DataMember
			Dim categoriesTable As ITable = TryCast(ds.Result(dataMember), ITable)
			For Each col As IColumn In categoriesTable.Columns
				If col.Type Is GetType(Byte()) Then
					Continue For
				End If
				Dim cell As New XRTableCell()
				cell.DataBindings.Add(New XRBinding("Text", Nothing, String.Format("{0}.{1}", dataMember, col.Name)))
				row.Cells.Add(cell)
			Next col
			tbl.Rows.Add(row)
			AddHandler tbl.BeforePrint, AddressOf tbl_BeforePrint
			tbl.AdjustSize()
			tbl.EndInit()
			Dim detailBand As DetailBand = Nothing
			If report.Bands(BandKind.Detail) Is Nothing Then
				report.Bands.Add(New DetailBand())
			End If
			detailBand = TryCast(report.Bands(BandKind.Detail), DetailBand)
			detailBand.HeightF = tbl.HeightF
			detailBand.Controls.Add(tbl)
		End Sub
		Private Shared Function GetDetailData() As SqlDataSource
			Dim ds As New SqlDataSource(New Access2007ConnectionParameters("..\..\App_Data\nwind.mdb", ""))
			ds.Queries.Add(New CustomSqlQuery("Products", "select * from Products"))
			ds.RebuildResultSchema()
			Return ds
		End Function
		#End Region
	End Class
End Namespace

