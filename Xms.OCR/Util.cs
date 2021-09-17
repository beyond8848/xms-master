using ImageMagick;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetCoreTest
{
    public static class Util
	{
		[DllImport("ocr_system.dll", EntryPoint = "Rec", SetLastError = true, CharSet = CharSet.Ansi)]
		static extern IntPtr Rec(byte[] input, int height, int width,StringBuilder configPath);

		public static DoubleList<OcrItem, int> getNearest(Box box, List<OcrItem> array, int num = -1)
		{
			if (num == -1)
				num = array.Count;
			DoubleList<OcrItem, int> item_distance = new DoubleList<OcrItem, int>();
			for (int i =0;i<array.Count;i++ )
			{
				OcrItem item = array[i];
				int dis = Distance(BoxToRect(box), item.rect);
				item_distance.Add(item, dis);
			}
			QuickSortStrict(item_distance);
			return item_distance.Top(num);
		}

		public static DoubleList<OcrItem, int> getBelowNearest(Box box, List<OcrItem> array, int num = -1)
		{
			if (num == -1)
				num = array.Count;
			DoubleList<OcrItem, int> item_distance = new DoubleList<OcrItem, int>();
			foreach (OcrItem item in array)
			{
				if (item.box.top > box.top)
					break;
				int dis = Below_Distance(BoxToRect(box), item.rect);
				item_distance.Add(item, dis);
			}
			QuickSortStrict(item_distance);
			return item_distance.Top(num);
		}

		static int Distance(Rect rect1, Rect rect2)
		{
			int min_dist;

			//首先计算两个矩形中心点
			OpenCvSharp.Point C1, C2;
			C1.X = rect1.X + (rect1.Width / 2);
			C1.Y = rect1.Y + (rect1.Height / 2);
			C2.X = rect2.X + (rect2.Width / 2);
			C2.Y = rect2.Y + (rect2.Height / 2);

			// 分别计算两矩形中心点在X轴和Y轴方向的距离
			int Dx, Dy;
			Dx = Math.Abs(C2.X - C1.X);
			Dy = Math.Abs(C2.Y - C1.Y);

			//两矩形不相交，在X轴方向有部分重合的两个矩形，最小距离是上矩形的下边线与下矩形的上边线之间的距离
			if ((Dx < ((rect1.Width + rect2.Width) / 2)) && (Dy >= ((rect1.Height + rect2.Height) / 2)))
			{
				min_dist = Dy - ((rect1.Height + rect2.Height) / 2);
			}

			//两矩形不相交，在Y轴方向有部分重合的两个矩形，最小距离是左矩形的右边线与右矩形的左边线之间的距离
			else if ((Dx >= ((rect1.Width + rect2.Width) / 2)) && (Dy < ((rect1.Height + rect2.Height) / 2)))
			{
				min_dist = Dx - ((rect1.Width + rect2.Width) / 2);
			}

			//两矩形不相交，在X轴和Y轴方向无重合的两个矩形，最小距离是距离最近的两个顶点之间的距离，
			// 利用勾股定理，很容易算出这一距离
			else if ((Dx >= ((rect1.Width + rect2.Width) / 2)) && (Dy >= ((rect1.Height + rect2.Height) / 2)))
			{
				int delta_x = Dx - ((rect1.Width + rect2.Width) / 2);
				int delta_y = Dy - ((rect1.Height + rect2.Height) / 2);
				min_dist = (int)Math.Sqrt(delta_x * delta_x + delta_y * delta_y);
			}

			//两矩形相交，最小距离为负值，返回-1
			else
			{
				min_dist = -1;
			}
			return min_dist;
		}

		static int Below_Distance(Rect rect1, Rect rect2)
		{
			int min_dist;

			//首先计算两个矩形中心点
			OpenCvSharp.Point C1, C2;
			C1.X = rect1.X + (rect1.Width / 2);
			C1.Y = rect1.Y + (rect1.Height / 2);
			C2.X = rect2.X + (rect2.Width / 2);
			C2.Y = rect2.Y + (rect2.Height / 2);

			// 分别计算两矩形中心点在X轴和Y轴方向的距离
			int Dx, Dy;
			Dx = Math.Abs(C2.X - C1.X);
			Dy = Math.Abs(C2.Y - C1.Y);

			//两矩形不相交，在X轴方向有部分重合的两个矩形，最小距离是上矩形的下边线与下矩形的上边线之间的距离
			if ((Dx < ((rect1.Width + rect2.Width) / 2)) && (Dy >= ((rect1.Height + rect2.Height) / 2)))
			{
				min_dist = Dy - ((rect1.Height + rect2.Height) / 2);
			}

			//两矩形不相交，在Y轴方向有部分重合的两个矩形，最小距离是左矩形的右边线与右矩形的左边线之间的距离
			else if ((Dx >= ((rect1.Width + rect2.Width) / 2)) && (Dy < ((rect1.Height + rect2.Height) / 2)))
			{
				//min_dist = Dx - ((rect1.Width + rect2.Width) / 2);
				min_dist = 99999999;
			}

			//两矩形不相交，在X轴和Y轴方向无重合的两个矩形，最小距离是距离最近的两个顶点之间的距离，
			// 利用勾股定理，很容易算出这一距离
			else if ((Dx >= ((rect1.Width + rect2.Width) / 2)) && (Dy >= ((rect1.Height + rect2.Height) / 2)))
			{
				//int delta_x = Dx - ((rect1.Width + rect2.Width) / 2);
				//int delta_y = Dy - ((rect1.Height + rect2.Height) / 2);
				//min_dist = (int)Math.Sqrt(delta_x * delta_x + delta_y * delta_y);
				min_dist = 99999999;
			}

			//两矩形相交，最小距离为负值，返回-1
			else
			{
				min_dist = -1;
			}
			return min_dist;
		}

		public static Rect BoxToRect(Box box)
		{
			Rect r = new Rect(box.left, box.top, box.right - box.left, box.bottom - box.top);
			return r;
		}

		static void QuickSortStrict(DoubleList<OcrItem, int> data)
		{
			QuickSortStrict(data, 0, data.Count - 1);
		}

		public static void QuickSortStrict(DoubleList<OcrItem, int> data, int low, int high)
		{
			if (low >= high) return;
			int temp = data[low].Value;
			int i = low + 1, j = high;
			while (true)
			{
				while (data[j].Value > temp) j--;
				while (data[i].Value < temp && i < j) i++;
				if (i >= j) break;
				data.Swap(i, j);
				i++; j--;
			}
			if (j != low)
				data.Swap(low, j);
			QuickSortStrict(data, j + 1, high);
			QuickSortStrict(data, low, j - 1);
		}

		public static string RegexMatch(IList<OcrItem> list, string reg)
		{
			string str = "";
			for (int i = 0; i < list.Count; i++)
			{
				Regex regex = new Regex(reg, RegexOptions.ECMAScript);
				MatchCollection matches = regex.Matches(list[i].str);
				if (matches.Count == 1)
				{
					str = matches[0].Value;
					break;
				}
				else if (matches.Count ==2)
				{
					str = matches[1].Value;
					break;
				}
            }
            return str;
		}

		public static Stream ConvertPDF2Image(string pdfInputPath)
		{
			MagickNET.SetGhostscriptDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
			MagickReadSettings settings = new MagickReadSettings();
			settings.Density = new Density(300, 300); //设置格式
			MagickImageCollection images = new MagickImageCollection();
			images.Read(pdfInputPath, settings);
			if (images.Count == 0)
				return null;
			MagickImage image = (MagickImage)images[0];
			if (image.HasAlpha)
				image.Alpha(AlphaOption.Background);
			image.Format = MagickFormat.Png;
			MemoryStream stream = new MemoryStream();
			image.Write(stream);
			return stream;
		}

		public static string GetValue(BoxEnum boxEnum, List<OcrItem> ocrItems)
		{
			DoubleList<OcrItem, int> list = getNearest(boxEnum.GetAttribute<BoxAttribute>().Box,
				ocrItems, 5);
			string str = RegexMatch(list.ItemList, boxEnum.GetAttribute<BoxAttribute>().Reg);
			return str;
		}


		public static Invoice OCR_Invoice(string filePath,string configPath,bool getPhoto)
		{
			Invoice invoice = new Invoice();
			#region 读取数据
			if (!File.Exists(filePath))
				return null;
			Mat mat;
			if (filePath.Substring(filePath.Length - 4) == ".pdf")
			{
				Stream stream = ConvertPDF2Image(filePath);
				if (stream == null)
					return null;
				if (getPhoto)
					invoice.Stream = stream;
				mat = Mat.FromStream(stream, ImreadModes.AnyColor);
			}
			else
				mat = Cv2.ImRead(filePath);
			#endregion
			#region OCR识别
			int stride;
			byte[] source = GetBGRValues(mat.ToBitmap(), out stride);
			float X_times = (float)mat.Width / 2598;
			float Y_times = (float)mat.Height / 1650;
			Box.InitTimes(X_times, Y_times); 
			StringBuilder stringBuilder = new StringBuilder(configPath);
			IntPtr reconginzedStringPtr = Rec(source, mat.Height, mat.Width,stringBuilder);
			mat.Dispose();
			byte[] bytes = System.Text.Encoding.Unicode.GetBytes(Marshal.PtrToStringUni(reconginzedStringPtr));//转成UNICODE编码
			string result = System.Text.Encoding.UTF8.GetString(bytes);//转成UTF8
			OcrJson ocr = JsonHelper.DeserializeJsonToObject<OcrJson>(result);
			#endregion
			#region 构建实体
			//判断发票抬头
			if (ocr == null)
				return null;
			string title = GetValue(BoxEnum.N_Ele_Title, ocr.array);
			if (!string.IsNullOrEmpty(title))
			{
				invoice.Normal = new NormalInvoice()
				{
					Title = title,
					Buyer = new Company()
					{
						Identification = GetValue(BoxEnum.N_B_Identification, ocr.array),
						Name = GetValue(BoxEnum.N_B_Name, ocr.array),
						Bank_ID = GetValue(BoxEnum.N_B_Bank, ocr.array),
						Location_Tel = GetValue(BoxEnum.N_B_Location, ocr.array),
					},
					Seller = new Company()
					{
						Identification = GetValue(BoxEnum.N_S_Identification, ocr.array),
						Name = GetValue(BoxEnum.N_S_Name, ocr.array),
						Bank_ID = GetValue(BoxEnum.N_S_Bank, ocr.array),
						Location_Tel = GetValue(BoxEnum.N_S_Location, ocr.array),
					},
					Items = new List<Project>()
					{
						new Project()
						{
							Trade = GetValue(BoxEnum.N_Trade, ocr.array),
							UnitPrice = GetValue(BoxEnum.N_UnitPrice, ocr.array),
							Count = GetValue(BoxEnum.N_Count, ocr.array),
							Sum = GetValue(BoxEnum.N_Sum, ocr.array),
							TaxRate = GetValue(BoxEnum.N_TaxRate, ocr.array),
							Tax = GetValue(BoxEnum.N_Tax, ocr.array),
						}
					},
					InvoiceID = GetValue(BoxEnum.N_InvoiceID, ocr.array),
					InvoiceNo = GetValue(BoxEnum.N_InvoiceNo, ocr.array),
					InvoicingDate = GetValue(BoxEnum.N_InvoicingDate, ocr.array),
					CheckID = GetValue(BoxEnum.N_CheckID, ocr.array),
					MachineID = GetValue(BoxEnum.N_MachineID, ocr.array),
					PriceTaxTotal_CHS = GetValue(BoxEnum.N_PriceTaxTotal_CHS, ocr.array),
					PriceTaxTotal_Num = GetValue(BoxEnum.N_PriceTaxTotal_Num, ocr.array),
					Recipient = GetValue(BoxEnum.N_Recipient, ocr.array),
					Checker = GetValue(BoxEnum.N_Checker, ocr.array),
					Invoicer = GetValue(BoxEnum.N_Invoicer, ocr.array),
				};
			}
			else if (!string.IsNullOrEmpty(""))
			{

			}
			#endregion
			GC.Collect();
			return invoice;
		}


		public static byte[] GetBGRValues(Bitmap bmp, out int stride)
		{
			var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
			var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
			stride = bmpData.Stride;
			var rowBytes = bmpData.Width * Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
			var imgBytes = bmp.Height * rowBytes;
			byte[] rgbValues = new byte[imgBytes];
			IntPtr ptr = bmpData.Scan0;
			for (var i = 0; i < bmp.Height; i++)
			{
				Marshal.Copy(ptr, rgbValues, i * rowBytes, rowBytes);
				ptr += bmpData.Stride;
			}
			bmp.UnlockBits(bmpData);
			return rgbValues;
		}

		public class DoubleList<T, Y> : IEnumerable
		{
			public IList<T> ItemList
			{
				get;
				private set;
			}

			public IList<Y> ValueList
			{
				get;
				private set;
			}

			public int Count
			{
				get
				{
					return ItemList.Count;
				}
			}

			public DoubleList()
			{
				ItemList = new List<T>();
				ValueList = new List<Y>();
			}

			public DoubleList(List<T> t, List<Y> y)
			{
				ItemList = t;
				ValueList = y;
			}

			//public Y this[T t]
			//{
			//	get
			//	{
			//		foreach (var item in ItemList)
			//		{
			//			if (item.Equals(t))
			//			{
			//				int index = ItemList.IndexOf(item);
			//				return ValueList[index];
			//			}
			//		}
			//		return default;
			//	}
			//}

			public Pair<T, Y> this[int i]
			{
				get
				{
					return new Pair<T, Y>(ItemList[i], ValueList[i]);
				}
				set
				{
					Pair<T, Y> pair = value;
					ItemList[i] = pair.Key;
					ValueList[i] = pair.Value;
				}
			}

			public void Add(T t, Y y)
			{
				ItemList.Add(t);
				ValueList.Add(y);
			}

			public void Add(Pair<T, Y> pair)
			{
				ItemList.Add(pair.Key);
				ValueList.Add(pair.Value);
			}

			public void Swap(int i, int j)
			{
				Pair<T, Y> i_pair = this[i];
				this[i] = this[j];
				this[j] = i_pair;
			}

			public DoubleList<T, Y> Top(int i)
			{
				DoubleList<T, Y> result = new DoubleList<T, Y>();
				for (int j = 0; j < i; j++)
				{
					result.Add(this[j]);
				}
				return result;
			}

			public T FirstOrDefault()
			{
				T t = ItemList.FirstOrDefault();
				return t;
			}

			public IEnumerator GetEnumerator()
			{
				for (int i = 0; i < this.Count; i++)
				{
					yield return new Pair<T, Y>(this.ItemList[i], this.ValueList[i]);
				}
			}

			public IList<T> Key
			{
				get;
				private set;
			}
		}

		public class Pair<T, Y>
		{
			public T Key { get; set; }
			public Y Value { get; set; }
			public Pair(T _Key, Y _Value)
			{
				Key = _Key;
				Value = _Value;
			}
		}

		public enum BoxEnum
		{
			[BoxAttribute(329, 35, 708, 68, @"[\u4e00-\u9fa5]{0,}电子[\u4e00-\u9fa5]{0,}发票")]
			N_Ele_Title,
			[BoxAttribute(1856, 130, 2270, 179, @"\d{12}$")]
			N_InvoiceID,
			[BoxAttribute(1859, 200, 2194, 244, @"\d{8}$")]
			N_InvoiceNo,
			[BoxAttribute(1859, 265, 2270, 309, @"\d{4}年\d{1,2}月\d{1,2}日")]
			N_InvoicingDate,
			[BoxAttribute(1859, 325, 2459, 369, @"\d{20}$")]
			N_CheckID,
			[BoxAttribute(121, 320, 538, 369, @"\d{12}$")]
			N_MachineID,
			[BoxAttribute(243, 458, 795, 502, @"[A-Za-z0-9]{18}")]
			N_B_Identification,
			[BoxAttribute(430, 401, 898, 445, @"[\u4e00-\u9fa5]{3,}")]
			N_B_Name,
			[BoxAttribute(243, 578, 1163, 626, @"[\u4e00-\u9fa50-9]{7,}")]
			N_B_Bank,
			[BoxAttribute(246, 521, 1363, 564, @"[\u4e00-\u9fa50-9\-]{3,}")]
			N_B_Location,
			[BoxAttribute(246, 1318, 830, 1362, @"[A-Za-z0-9]{18}")]
			N_S_Identification,
			[BoxAttribute(427, 1259, 822, 1305, @"[\u4e00-\u9fa5]{3,}")]
			N_S_Name,
			[BoxAttribute(243, 1432, 928, 1484, @"[\u4e00-\u9fa50-9]{7,}")]
			N_S_Bank,
			[BoxAttribute(243, 1375, 1252, 1422, @"[\u4e00-\u9fa50-9\-]{3,}")]
			N_S_Location,
			[BoxAttribute(140, 705, 533, 746, @"[\u4E00-\u9FA5A-Za-z0-9*/\-\+%&',;=?$\x22]{2,}")]
			N_Trade,
			[BoxAttribute(1585, 705, 1677, 743, @"[0-9.]{1,}")]
			N_UnitPrice,
			[BoxAttribute(1363, 702, 1420, 743, @"[0-9.]{1,}")]
			N_Count,
			[BoxAttribute(1897, 705, 1989, 746, @"[0-9.]{1,}")]
			N_Sum,
			[BoxAttribute(2075, 702, 2132, 746, @"[0-9%.]{1,}")]
			N_TaxRate,
			[BoxAttribute(2370, 700, 2454, 749, @"[0-9.]{1,}")]
			N_Tax,
			[BoxAttribute(752, 1188, 966, 1234, @"[\u4e00-\u9fa5]{1,}(?=元|角|分){1,}[\u4e00-\u9fa5]{1,}")]
			N_PriceTaxTotal_CHS,
			[BoxAttribute(2324, 1194, 2443, 1232, @"[$￥]*[ ]*[0-9\.]{1,}")]
			N_PriceTaxTotal_Num,
			[BoxAttribute(181, 1506, 389, 1557, @"[\u4e00-\u9fa5]{1,}")]
			N_Recipient,
			[BoxAttribute(792, 1503, 998, 1557, @"[\u4e00-\u9fa5]{1,}")]
			N_Checker,
			[BoxAttribute(1393, 1514, 1637, 1560, @"[\u4e00-\u9fa5]{1,}")]
			N_Invoicer,

		}
		public class BoxAttribute : Attribute
		{
			internal BoxAttribute(int _left, int _top, int _right, int _bottom, string _reg)
			{
				this.Box = new Box(Convert.ToInt32( _left * Box.X_times), Convert.ToInt32(_top * Box.Y_times), Convert.ToInt32(_right * Box.X_times), Convert.ToInt32(_bottom * Box.Y_times));
				this.Reg = _reg;
			}
			public Box Box { get; private set; }
			public string Reg { get; private set; }
		}
		public static TAttribute GetAttribute<TAttribute>(this Enum value)
			  where TAttribute : Attribute
		{
			var type = value.GetType();
			var name = Enum.GetName(type, value);
			return type.GetField(name)
				.GetCustomAttributes(false)
				.OfType<TAttribute>()
				.SingleOrDefault();
		}
	}
}
