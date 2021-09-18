using System.Collections.Generic;
using System.IO;

namespace Xms.OCR
{
    public class Invoice
    {
        public TrainTicket Train { get; set; }

        public PlaneTicket Plane { get; set; }

        public BusTicket Bus { get; set; }

        public NormalInvoice Normal { get; set; }

        public Stream Stream { get; set; }
    }

    public class TrainTicket
    {

    }
    public class PlaneTicket
    {

    }

    public class BusTicket
    {

    }

    public class NormalInvoice
    {
        /// <summary>
        /// 购买方
        /// </summary>
        public Company Buyer { get; set; }
        /// <summary>
        /// 销售方
        /// </summary>
        public Company Seller { get; set; }
        /// <summary>
        /// 发票抬头
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 机器编号
        /// </summary>
        public string MachineID { get; set; }
        /// <summary>
        /// 发票代码
        /// </summary>
        public string InvoiceID { get; set; }
        /// <summary>
        /// 发票号码
        /// </summary>
        public string InvoiceNo { get; set; }
        /// <summary>
        /// 开票日期
        /// </summary>
        public string InvoicingDate { get; set; }
        /// <summary>
        /// 校验码
        /// </summary>
        public string CheckID { get; set; }
        /// <summary>
        /// 收款人
        /// </summary>
        public string Recipient { get; set; }
        /// <summary>
        /// 复核
        /// </summary>
        public string Checker { get; set; }
        /// <summary>
        /// 开票人
        /// </summary>
        public string Invoicer { get; set; }
        /// <summary>
        /// 价税合计(中文大写)
        /// </summary>
        public string PriceTaxTotal_CHS { get; set; }
        /// <summary>
        /// 价税合计(数字)
        /// </summary>
        public string PriceTaxTotal_Num { get; set; }
        /// <summary>
        /// 项目
        /// </summary>
        public List<Project> Items { get; set; }
    }

    public class Company
    {
        /// <summary>
        /// 公司名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 纳税人识别号
        /// </summary>
        public string Identification { get; set; }
        /// <summary>
        /// 地址_电话
        /// </summary>
        public string Location_Tel { get; set; }
        /// <summary>
        /// 开户行_账号
        /// </summary>
        public string Bank_ID { get; set; }
    }

    public class Project
    {
        /// <summary>
        /// 商品服务项目名称
        /// </summary>
        public string Trade { get; set; }
        /// <summary>
        /// 单价
        /// </summary>
        public string UnitPrice { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public string Count { get; set; }
        /// <summary>
        /// 税率
        /// </summary>
        public string TaxRate { get; set; }
        /// <summary>
        /// 金额
        /// </summary>
        public string Sum { get; set; }
        /// <summary>
        /// 税额
        /// </summary>
        public string Tax { get; set; }
    }
}
