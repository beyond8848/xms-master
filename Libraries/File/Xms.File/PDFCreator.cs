using System;
using System.Collections.Generic;
using System.Text;

namespace Xms.File
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Reflection;
    using iTextSharp.text;
    using iTextSharp.text.pdf;
    using Xms.Flow.Abstractions;
    using Xms.Flow.Domain;
    using Xms.Logging.AppLog;

    public class PDFCreator<T>
    {
        private static Document _doc;
        //中文字体-宋体
        private static string fontCHN = System.AppDomain.CurrentDomain.BaseDirectory + "\\simsun.ttf";
        private static string fontdb = @"c:\Windows\fonts\SIMSUN.TTC,1";
        private static BaseFont bfCHN = BaseFont.CreateFont(fontdb, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

        //宋体标题-深灰色
        private static Font fontTitle = new Font(bfCHN, (float)20, 1, BaseColor.DARK_GRAY);

      

        //宋体正文内容
        private static Font fontContent = new Font(BaseFont.CreateFont(fontCHN, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED), (float)10, 1, BaseColor.DARK_GRAY);

        //宋体正文内容-下划线字体
        private static Font fontContentUnderline = new Font(BaseFont.CreateFont(fontCHN, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED), (float)10, 1, BaseColor.DARK_GRAY);

        //宋体正文内容-红色
        private static Font fontContentRed = new Font(BaseFont.CreateFont(fontCHN, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED), (float)10, 1, BaseColor.RED);



        public string CreateRebursmentCertifyPDF(ILogService logService,string filePath, ArchiveItem archiveItem)
        {
            try
            {
                _doc = new Document(PageSize.A4);//默认边距，36磅
                FileStream fs = new FileStream(filePath, FileMode.Create);
                PdfWriter writer = PdfWriter.GetInstance(_doc, fs);
                writer.CloseStream = false;//把doc内容写入流中
                _doc.Open();
                CreatePageHeader();
                //_doc.AddTitle("电子发票流程报销凭证");
                //_doc.Add(new Paragraph("电子发票流程报销凭证"));
                CreateTextAlignCenter("电子发票流程报销凭证");
                //生成报销凭证
                CreateTableForRebursment(archiveItem);
                _doc.Close();//关闭文档
                //保存PDF文件
                MemoryStream ms = new MemoryStream();
                if (fs != null)
                {
                    byte[] bytes = new byte[fs.Length];//定义一个长度为fs长度的字节数组
                    fs.Read(bytes, 0, (int)fs.Length);//把fs的内容读到字节数组中
                    ms.Write(bytes, 0, bytes.Length);//把字节内容读到流中
                    fs.Flush();
                    fs.Close();
                }
                ms.Close();
                ms.Dispose();

            }
            catch(Exception ex)
            {
                logService.Error("PDFCreator CreateRebursmentCertifyPDF method throws exception", ex);
            }
            return filePath;
        }

        /// <summary>
        /// 生成流程明细PDF文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath">PDF文件所存位置</param>
        /// <param name="items">流程明细条目</param>
        /// <param name="floats">表格宽度每列宽度</param>
        /// <returns></returns>
        public string CreateWorkFlowPDFForMultipleWorkFlowInsance(ILogService logService ,string filePath, List<WorkFlowInstance> items)
        {
            try
            {
                _doc = new Document(PageSize.A4);//默认边距，36磅
                FileStream fs = new FileStream(filePath, FileMode.Create);
                PdfWriter writer = PdfWriter.GetInstance(_doc, fs);
                writer.CloseStream = false;//把doc内容写入流中
                _doc.Open();
                //创建页眉+图片+下划线
                CreatePageHeader();
                //_doc.AddTitle("电子发票流程报销信息");
                CreateTextAlignCenter("电子发票流程报销信息");
                //_doc.Add(new Paragraph("电子发票流程报销信息"));
                foreach (var workflowInstance in items)
                {
                    List<WorkFlowTinyInfo> workFlowTinyInfos = new List<WorkFlowTinyInfo>();
                    WorkFlowTinyInfo workFlowTinyInfo = new WorkFlowTinyInfo
                    {
                        Description = workflowInstance.Description,
                        HandleName = "发起申请",
                        HandlerIdName = workflowInstance.ApplicantName,
                        processedTime = workflowInstance.CreatedOn,
                        Status = "已发起"
                    };
                    workFlowTinyInfos.Add(workFlowTinyInfo);
                    foreach (var workflowinfo in workflowInstance.Steps)
                    {
                         workFlowTinyInfo = new WorkFlowTinyInfo
                        {
                            Description = workflowinfo.Description,
                            HandleName = workflowinfo.Name,
                            HandlerIdName = workflowinfo.HandlerIdName,
                            Status = GetStatusDesc(workflowinfo.StateCode),
                            processedTime = workflowinfo.HandleTime.Value
                        };

                        workFlowTinyInfos.Add(workFlowTinyInfo);
                    }
                    CreateTable(workFlowTinyInfos, new float[5] { 100, 100, 100, 100, 100 });
                }
                
                //AddPageNumberContent(writer, 1, 1); //添加页码
                _doc.Close();//关闭文档
                //保存PDF文件
                MemoryStream ms = new MemoryStream();
                if (fs != null)
                {
                    byte[] bytes = new byte[fs.Length];//定义一个长度为fs长度的字节数组
                    fs.Read(bytes, 0, (int)fs.Length);//把fs的内容读到字节数组中
                    ms.Write(bytes, 0, bytes.Length);//把字节内容读到流中
                    fs.Flush();
                    fs.Close();
                }
                ms.Close();
                ms.Dispose();
            }
            catch (DocumentException ex)
            {
                logService.Error("PDFCreator CreateWorkFlowPDFForMultipleWorkFlowInsance method throws exception", ex);
            }
            return filePath;
        }

        private string GetStatusDesc(WorkFlowProcessState StateCode)
        {
            string statusDesc = "";
            if (StateCode == WorkFlowProcessState.Passed)
            {
                statusDesc = "通过";
            }
            else if (StateCode == WorkFlowProcessState.Canceled)
            {
                statusDesc = "取消";
            }
            else if (StateCode == WorkFlowProcessState.Disabled)
            {
                statusDesc = "禁用";
            }
            else if (StateCode == WorkFlowProcessState.Processing)
            {
                statusDesc = "处理中";
            }
            else if (StateCode == WorkFlowProcessState.UnPassed)
            {
                statusDesc = "未通过";
            }
            else if (StateCode == WorkFlowProcessState.Waiting)
            {
                statusDesc = "等待";
            }

            return statusDesc;
        }


        /// <summary>
        /// 生成流程明细PDF文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath">PDF文件所存位置</param>
        /// <param name="items">流程明细条目</param>
        /// <param name="floats">表格宽度每列宽度</param>
        /// <returns></returns>
        public string CreateWorkFlowPDF<T>(string filePath,List<T> items,float[] floats)
        {
            try
            {
                //A4纸尺寸：595*420，单位：磅
                _doc = new Document(PageSize.A4);//默认边距，36磅
                //_doc = new Document(PageSize.A5, 36, 36, 36, 36);
                //doc.SetMargins(0, 0, 0, 0);//移除页边距
                FileStream fs = new FileStream(filePath, FileMode.Create);
                PdfWriter writer = PdfWriter.GetInstance(_doc, fs);
                writer.CloseStream = false;//把doc内容写入流中
                _doc.Open();
                //创建页眉+图片+下划线
                CreatePageHeader();
                //_doc.AddTitle("电子发票流程报销信息");
                CreateTextAlignCenter("电子发票流程报销信息");

                CreateTable(items, new float[5] { 100, 100, 100,100,100 });
                //AddPageNumberContent(writer, 1, 1); //添加页码
                _doc.Close();//关闭文档
                //保存PDF文件
                MemoryStream ms = new MemoryStream();
                if (fs != null)
                {
                    byte[] bytes = new byte[fs.Length];//定义一个长度为fs长度的字节数组
                    fs.Read(bytes, 0, (int)fs.Length);//把fs的内容读到字节数组中
                    ms.Write(bytes, 0, bytes.Length);//把字节内容读到流中
                    fs.Flush();
                    fs.Close();
                }
            }
            catch (DocumentException ex)
            {
              //
            }
            return filePath;
        }

        private static void CreateTextAlignCenter(string txt)
        {
            PdfPTable table = new PdfPTable(1);//一个单元格的
            table.TotalWidth = 500;//设置绝对宽度
            table.LockedWidth = true;//使绝对宽度模式生效
            table.PaddingTop = 0;

            PdfPCell cell = new PdfPCell(new Phrase(txt, fontTitle));
            cell.BorderWidth = 0f;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.BorderWidthBottom = 0.1f;//底部画线
            cell.FixedHeight = 50.00f;
            cell.BorderColorBottom = BaseColor.DARK_GRAY;

            table.AddCell(cell);
            _doc.Add(table);
        }




        /// <summary>
        /// 设置水印
        /// </summary>
        /// <param name="pdfFilePath"></param>
        private void SetWaterMark(string pdfFilePath)
        {
            PdfReader reader = null;
            PdfStamper stamper = null;
            string newPDFFileName = "";

            try
            {
                reader = new PdfReader(pdfFilePath);

                string waterPDF = Path.GetDirectoryName(pdfFilePath);
                string fileWater = Path.GetFileName(pdfFilePath).Replace(".pdf", "") + "-Stamper.pdf";

                newPDFFileName = Path.Combine(waterPDF, fileWater);

                stamper = new PdfStamper(reader, new FileStream(newPDFFileName, FileMode.OpenOrCreate));

                string imgPath = AppDomain.CurrentDomain.BaseDirectory + "\\logo.png";

                iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(imgPath);

                float width = _doc.PageSize.Width;
                float height = _doc.PageSize.Height;

                float waterMarkWidth = 150;

                //像素
                float percent = waterMarkWidth / img.Width;
                img.ScaleAbsoluteWidth(waterMarkWidth);//缩放图片指定宽度
                img.ScaleAbsoluteHeight(img.Height * percent);//等比例缩放高度
                img.SetAbsolutePosition((width - waterMarkWidth) / 2, (height - img.Height * percent) / 2);//设置水印位置
                img.Rotation = 95;//旋转角度

                //指定颜色透明,如：白色
                img.Transparency = new int[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

                //设置透明度
                //create new graphics state and assign opacity
                PdfGState graphicsState = new PdfGState();
                graphicsState.FillOpacity = 0.2F; // (or whatever)
                                                  //set graphics state to pdfcontentbyte
                PdfContentByte contentByte;
                int j = 0;
                int n = reader.NumberOfPages;
                while (j < n)
                {
                    j++;
                    contentByte = stamper.GetOverContent(j);
                    contentByte.SetGState(graphicsState);//设置透明度
                    contentByte.AddImage(img);
                }
            }
            finally
            {
                if (stamper != null) stamper.Close();
                if (stamper != null) reader.Close();

                //改名
                if (File.Exists(newPDFFileName))
                {
                    File.Delete(pdfFilePath);
                    FileInfo fi = new FileInfo(newPDFFileName);
                    fi.MoveTo(pdfFilePath);
                }
            }
        }


        private static void CreatePageHeader()
        {
            PdfPTable table = new PdfPTable(1);//一个单元格的
            table.TotalWidth = 500;//设置绝对宽度
            table.LockedWidth = true;//使绝对宽度模式生效
            table.PaddingTop = 0;

            PdfPCell cell = new PdfPCell();

            string logoPath = AppDomain.CurrentDomain.BaseDirectory + "\\logo.png";

            if(File.Exists(logoPath))
            {
                Image gif = Image.GetInstance(logoPath);
                gif.ScaleAbsoluteWidth(100);//缩放图片
                cell.AddElement(gif);
                cell.BorderWidth = 0f;
                cell.BorderWidthBottom = 0.1f;//底部画线
                cell.BorderColorBottom = BaseColor.DARK_GRAY;
            }

            table.AddCell(cell);
            _doc.Add(table);
        }

        /// <summary>
        /// 在固定位置生成页码
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="totalPage"></param>
        /// <param name="currentPage"></param>
        private static void AddPageNumberContent(PdfWriter writer, int totalPage, int currentPage)
        {
            //string text = String.Format("共 {0} 页 第 {1} 页", totalPage, currentPage);
            string text = String.Format("第 {0} 页", currentPage);
            ColumnText ct = new ColumnText(writer.DirectContent);
            ct.SetSimpleColumn(new Rectangle(200, 0, 533, 50));
            ct.AddElement(new Paragraph(text, fontContentRed));
            ct.Go();
        }

        /// <summary>
        /// 在PDF页面固定位置输出文本
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="text"></param>
        /// <param name="rect"></param>
        /// <param name="font"></param>
        private void CreateText(PdfWriter writer, string text, Rectangle rect, Font font)
        {
            //在固定位置显示文本
            ColumnText ct = new ColumnText(writer.DirectContent);
            ct.SetSimpleColumn(rect);
            ct.AddElement(new Paragraph(text, font));
            ct.Go();
        }

        private void CreateTableForRebursment(ArchiveItem archiveItem)
        {
           
            iTextSharp.text.pdf.PdfPTable datatable = new PdfPTable(4);
            float[] headerwidths = { 50, 200, 50, 200};
            datatable.SetWidths(headerwidths);
            datatable.WidthPercentage = 100;
            datatable.HeaderRows = 1;
            datatable.PaddingTop = 5;

            Phrase ph;
            ph = new Phrase("报销人", fontContent);
            iTextSharp.text.pdf.PdfPCell cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 30;
            cell1.BackgroundColor = new iTextSharp.text.BaseColor(0xC0, 0xC0, 0xC0);
            datatable.AddCell(cell1);

            ph = new Phrase(archiveItem.Claimer, fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 30;
            //cell1.BackgroundColor = new iTextSharp.text.BaseColor(0x00, 0x00, 0x00);
            datatable.AddCell(cell1);


            ph = new Phrase("报销时间", fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 30;
            cell1.BackgroundColor = new iTextSharp.text.BaseColor(0xC0, 0xC0, 0xC0);
            datatable.AddCell(cell1);

            ph = new Phrase(archiveItem.ApplicationTime.ToString(), fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 30;
            //cell1.BackgroundColor = new iTextSharp.text.BaseColor(0x00, 0x00, 0x00);
            datatable.AddCell(cell1);


            ph = new Phrase("所在部门", fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 30;
            cell1.BackgroundColor = new iTextSharp.text.BaseColor(0xC0, 0xC0, 0xC0);
            datatable.AddCell(cell1);

            ph = new Phrase(archiveItem.Department, fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 30;
            //cell1.BackgroundColor = new iTextSharp.text.BaseColor(0x00, 0x00, 0x00);
            datatable.AddCell(cell1);

            ph = new Phrase("报销金额", fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 30;
            cell1.BackgroundColor = new iTextSharp.text.BaseColor(0xC0, 0xC0, 0xC0);
            datatable.AddCell(cell1);

            ph = new Phrase(archiveItem.Amount.ToString(), fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 30;
            //cell1.BackgroundColor = new iTextSharp.text.BaseColor(0x00, 0x00, 0x00);
            datatable.AddCell(cell1);


            ph = new Phrase("名称", fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 60;
            cell1.BackgroundColor = new iTextSharp.text.BaseColor(0xC0, 0xC0, 0xC0);
            datatable.AddCell(cell1);

            ph = new Phrase(archiveItem.Title, fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 60;
            //cell1.BackgroundColor = new iTextSharp.text.BaseColor(0x00, 0x00, 0x00);
            datatable.AddCell(cell1);


            ph = new Phrase("事由", fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 60;
            cell1.BackgroundColor = new iTextSharp.text.BaseColor(0xC0, 0xC0, 0xC0);
            datatable.AddCell(cell1);

            ph = new Phrase(archiveItem.Reason, fontContent);
            cell1 = new PdfPCell(ph);
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            cell1.FixedHeight = 60;
            //cell1.BackgroundColor = new iTextSharp.text.BaseColor(0x00, 0x00, 0x00);
            datatable.AddCell(cell1);
            _doc.Add(datatable);
        }

        /// <summary>
        /// 给流程节点审批信息做节点。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="headerwidths"></param>
        private void CreateTable<T>(List<T> list, float[] headerwidths)
        {
            if (list == null || list.Count == 0) return;
            object o = list[0];
            PropertyInfo[] propertyInfos = o.GetType().GetProperties();
            iTextSharp.text.pdf.PdfPTable datatable = new PdfPTable(propertyInfos.Length);
            //float[] headerwidths = { 50, 100, 40, 40, 100, 80 };
            datatable.SetWidths(headerwidths);
            datatable.WidthPercentage = 100;
            datatable.HeaderRows = 1;
            datatable.PaddingTop = 5;


            for (int i = 0; i <= propertyInfos.Length - 1; i++)
            {
                Phrase ph;
                if (i==0)
                    ph = new Phrase("节点名称", fontContent);
                else if(i==1)
                    ph = new Phrase("审批人用户名", fontContent);
                else if(i==2)
                    ph = new Phrase("审批描述", fontContent);
                else if(i==3)
                    ph = new Phrase("流程状态", fontContent);
                else if (i == 4)
                    ph = new Phrase("审批时间", fontContent);
                else
                    ph = new Phrase(propertyInfos[i].Name, fontContent);

                iTextSharp.text.pdf.PdfPCell cell1 = new PdfPCell(ph);
                cell1.HorizontalAlignment = Element.ALIGN_CENTER;
                cell1.FixedHeight = 30;
                cell1.BackgroundColor = new iTextSharp.text.BaseColor(0xC0, 0xC0, 0xC0);
                datatable.AddCell(cell1);
            }

            for (int k = 0; k < list.Count; k++)
            {
                o = list[k];
                propertyInfos = o.GetType().GetProperties();
                foreach (var pi in propertyInfos)
                {
                    Phrase dataPh = new Phrase(pi.GetValue(o).ToString(), fontContent);
                    iTextSharp.text.pdf.PdfPCell cell1 = new PdfPCell(dataPh);
                    cell1.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell1.FixedHeight = 50;
                    datatable.AddCell(cell1);
                }
            }
            _doc.Add(datatable);
        }

        /// <summary>
        /// 在PDF生成表格
        /// </summary>
        private void CreateTable(DataTable dt)
        {
            iTextSharp.text.pdf.PdfPTable datatable = new PdfPTable(6);
            float[] headerwidths = { 50, 100, 40, 40, 100, 80 };
            datatable.SetWidths(headerwidths);
            datatable.WidthPercentage = 100;
            datatable.HeaderRows = 1;
            datatable.PaddingTop = 5;

            for (int i = 0; i <= dt.Columns.Count - 1; i++)
            {
                Phrase ph = new Phrase(dt.Columns[i].ColumnName, fontContent);
                iTextSharp.text.pdf.PdfPCell cell1 = new PdfPCell(ph);
                cell1.HorizontalAlignment = Element.ALIGN_CENTER;
                cell1.BackgroundColor = new iTextSharp.text.BaseColor(0xC0, 0xC0, 0xC0);
                datatable.AddCell(cell1);
            }

            foreach (DataRow R in dt.Rows)
            {
                datatable.AddCell(new Phrase(R["T1"].ToString(), fontContent));
                datatable.AddCell(new Phrase(R["T2"].ToString(), fontContent));
                datatable.AddCell(new Phrase(R["T3"].ToString(), fontContent));
                datatable.AddCell(new Phrase(R["T4"].ToString(), fontContent));
                datatable.AddCell(new Phrase(R["T5"].ToString(), fontContent));
                datatable.AddCell(new Phrase(R["T6"].ToString(), fontContent));
            }

            _doc.Add(datatable);
        }

    }


    public class PDFEntity
    {
        public string PdfName { get; set; }

        public string Size { get; set; }


        public string Path { get; set; }
    }
}
