using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Printing;
using DevExpress.XtraReports.UI;
using DevExpress.DataAccess.Sql;
using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql.DataApi;
using DevExpress.XtraReports.Parameters;
using DevExpress.XtraPrinting;
using DevExpress.Xpf.Printing.PreviewControl.Native.Models;
using System.Reflection;

namespace dxWPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow {
        public MainWindow() {
            InitializeComponent();
        }
        XtraReport masterReport;
        XtraReport detailReport;
        int currentPageIndex;
        private void Button_Click(object sender, RoutedEventArgs e) {
            Window w = new Window();
            DocumentPreviewControl preview = new DocumentPreviewControl();
            preview.DocumentPreviewMouseClick += preview_DocumentPreviewMouseClick;
            w.Content = preview;
            masterReport = GetMasterReport();
            preview.DocumentSource = masterReport;
            masterReport.CreateDocument();
            w.Show();
        }

        void preview_DocumentPreviewMouseClick(DependencyObject d, DocumentPreviewMouseEventArgs e) {
            DocumentPreviewControl preview = e.OriginalSource as DocumentPreviewControl;
            ReportDocumentViewModel model = preview.Document as ReportDocumentViewModel;
            PropertyInfo pi = model.GetType().GetProperty("Report", BindingFlags.NonPublic | BindingFlags.Public
        | BindingFlags.Instance | BindingFlags.Static);
            
            XtraReport currentReport = pi.GetValue(model, null) as XtraReport;
            if (currentReport == null)
            return;
            if(String.Equals(currentReport.Tag.ToString(), "Master")) {
                if (e.ElementTag != null && !String.IsNullOrEmpty(e.ElementTag.ToString())) {
                    string categoryName = (e.Brick as VisualBrick).Text;
                    int categoryID = Convert.ToInt32(e.ElementTag);
                    currentPageIndex = e.PageIndex + 1; 
                    XtraReport detailReport = GetDetailReport(categoryName, categoryID);
                    preview.DocumentSource = detailReport;
                }
            }
            if (String.Equals(currentReport.Tag.ToString(), "Detail")) {
                preview.DocumentSource = null;
                preview.DocumentSource = masterReport;
                masterReport.CreateDocument();
                preview.CurrentPageNumber = currentPageIndex;
            }
        }
        private XtraReport GetMasterReport() {
            if(masterReport == null)
                CreateMasterReport();
            return masterReport;
        }
        #region *****Master Report Initialization*****;
        private void CreateMasterReport() {
            SqlDataSource ds = GetMasterData();
            masterReport = new XtraReport();
            masterReport.Tag = "Master";
            masterReport.DataSource = ds;
            masterReport.DataMember = "Categories";
            CreateMasterReportControls(masterReport);
        }
        private void CreateMasterReportControls(XtraReport report) {
            XRTable tbl = new XRTable();
            tbl.BeginInit();            
            XRTableRow row = new XRTableRow();            
            row.HeightF = 40;
            SqlDataSource ds = report.DataSource as SqlDataSource;
            string dataMember = report.DataMember;
            ITable categoriesTable = ds.Result[dataMember] as ITable;
            foreach (IColumn col in categoriesTable.Columns) {
                if (col.Type == typeof(byte[]))
                    continue;
                XRTableCell cell = new XRTableCell();
                cell.DataBindings.Add(new XRBinding("Text", null, string.Format("{0}.{1}", dataMember, col.Name)));
                if (col.Name == "CategoryName") {
                    cell.Font = new DevExpress.Drawing.DXFont(cell.Font.Name, 14f, DevExpress.Drawing.DXFontStyle.Underline | DevExpress.Drawing.DXFontStyle.Bold);                    
                    cell.DataBindings.Add(new XRBinding("Tag", null, string.Format("{0}.{1}", dataMember, "CategoryID")));
                    cell.ForeColor = System.Drawing.Color.Blue;
                    cell.NavigateUrl = " "; 
                }
                row.Cells.Add(cell);
            }
            tbl.Rows.Add(row);
            tbl.BeforePrint += tbl_BeforePrint;
            tbl.AdjustSize();
            tbl.EndInit();
            DetailBand detailBand = null;
            if (report.Bands[BandKind.Detail] == null) {
                report.Bands.Add(new DetailBand());
            }
            detailBand = report.Bands[BandKind.Detail] as DetailBand;
            detailBand.HeightF = tbl.HeightF + 2;
            detailBand.PageBreak = PageBreak.AfterBand;
            detailBand.Controls.Add(tbl);
        }
        void tbl_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) {
            XRTable table = ((XRTable)sender);
            table.LocationF = new DevExpress.Utils.PointFloat(0F, 0F);
            
            table.WidthF = ((sender as XRTable).Report as XtraReport).PageWidth - ((sender as XRTable).Report as XtraReport).Margins.Left - ((sender as XRTable).Report as XtraReport).Margins.Right;
        }
        private static SqlDataSource GetMasterData() {
            SqlDataSource ds = new SqlDataSource(new Access2007ConnectionParameters(@"..\..\App_Data\nwind.mdb", ""));
            ds.Queries.Add(new CustomSqlQuery("Categories", "select * from Categories"));
            ds.RebuildResultSchema();
            return ds;
        }
        #endregion
        #region *****Detail Report Initialization*****
        private XtraReport GetDetailReport(string categoryName, int categoryID) {
            if (detailReport == null)
                CreateDetailReport();
            detailReport.Parameters["catIDParam"].Value = categoryID;
            detailReport.Parameters["catNameParam"].Value = categoryName;
            detailReport.CreateDocument();
            return detailReport;
        }
        private void CreateDetailReport() {
            detailReport = new XtraReport();
            detailReport.Tag = "Detail";
            detailReport.DataSource = GetDetailData();
            detailReport.DataMember = "Products";
            CreateDetailReportParameters(detailReport);
            detailReport.FilterString = string.Format("[CategoryID] = ?catIDParam");
            CreateDetailReportControls(detailReport);
        }
        private void CreateDetailReportParameters(XtraReport detailReport) {
            Parameter categoryNameParameter = new DevExpress.XtraReports.Parameters.Parameter();
            categoryNameParameter.Visible = false;
            categoryNameParameter.Name = "catNameParam";
            categoryNameParameter.Type = typeof(string);
            detailReport.Parameters.Add(categoryNameParameter);
            Parameter categoryIDParameter = new DevExpress.XtraReports.Parameters.Parameter();
            categoryIDParameter.Visible = false;
            categoryIDParameter.Name = "catIDParam";
            categoryIDParameter.Type = typeof(int);
            detailReport.Parameters.Add(categoryIDParameter);
        }
        private void CreateDetailReportControls(XtraReport report) {
            XRLabel titleLbl = new XRLabel() {
                LocationF = new System.Drawing.PointF(0, 0),
                SizeF = new System.Drawing.SizeF(600, 30),                
                Font = new DevExpress.Drawing.DXFont("Arial", 18f, DevExpress.Drawing.DXFontStyle.Bold | DevExpress.Drawing.DXFontStyle.Italic),
                ForeColor = System.Drawing.Color.Red
            };
            titleLbl.DataBindings.Add(new XRBinding(report.Parameters["catNameParam"], "Text", "Details for {0} category"));
            ReportHeaderBand reportHeaderBand = null;
            if (report.Bands[BandKind.ReportHeader] == null) {
                report.Bands.Add(new ReportHeaderBand());
            }            
            reportHeaderBand = report.Bands[BandKind.ReportHeader] as ReportHeaderBand;
            reportHeaderBand.HeightF = titleLbl.HeightF + 10;
            reportHeaderBand.Controls.Add(titleLbl);
            
            XRLabel homeLabel = new XRLabel() {
                LocationF = new System.Drawing.PointF(0, 0),
                SizeF = new System.Drawing.SizeF(400, 30),
                Text = "Back to categories",
                Font = new DevExpress.Drawing.DXFont("Arial", 16f, DevExpress.Drawing.DXFontStyle.Bold | DevExpress.Drawing.DXFontStyle.Italic | DevExpress.Drawing.DXFontStyle.Underline),
                NavigateUrl = " ",
                ForeColor = System.Drawing.Color.Blue
            };
            PageHeaderBand pageHeaderBand = null;
            if (report.Bands[BandKind.PageHeader] == null) {
                report.Bands.Add(new PageHeaderBand());
            }
            pageHeaderBand = report.Bands[BandKind.PageHeader] as PageHeaderBand;
            pageHeaderBand.HeightF = titleLbl.HeightF;
            pageHeaderBand.Controls.Add(homeLabel);

            XRTable tbl = new XRTable();
            tbl.BeginInit();
            tbl.Borders = DevExpress.XtraPrinting.BorderSide.All;
            XRTableRow row = new XRTableRow();
            row.HeightF = 40;
            SqlDataSource ds = report.DataSource as SqlDataSource;
            string dataMember = report.DataMember;
            ITable categoriesTable = ds.Result[dataMember] as ITable; 
            foreach (IColumn col in categoriesTable.Columns) {
                if (col.Type == typeof(byte[]))
                    continue;
                XRTableCell cell = new XRTableCell();
                cell.DataBindings.Add(new XRBinding("Text", null, string.Format("{0}.{1}", dataMember, col.Name)));
                row.Cells.Add(cell);
            }
            tbl.Rows.Add(row);
            tbl.BeforePrint += tbl_BeforePrint;
            tbl.AdjustSize();
            tbl.EndInit();
            DetailBand detailBand = null;
            if (report.Bands[BandKind.Detail] == null) {
                report.Bands.Add(new DetailBand());
            }
            detailBand = report.Bands[BandKind.Detail] as DetailBand;
            detailBand.HeightF = tbl.HeightF;
            detailBand.Controls.Add(tbl);
        }
        private static SqlDataSource GetDetailData() {
            SqlDataSource ds = new SqlDataSource(new Access2007ConnectionParameters(@"..\..\App_Data\nwind.mdb", ""));
            ds.Queries.Add(new CustomSqlQuery("Products", "select * from Products"));
            ds.RebuildResultSchema();
            return ds;
        }
        #endregion
    }
}

