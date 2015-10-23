using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace PhoneGeoLib
{
    public static class Initialize
    {
        private static async Task<List<string>> GetCity(string tel)
        {
            var returnStr = new List<string>();
            var respJson = string.Empty;
            var apiKey = Utils.ApiKey;

            try
            {
                respJson =
                    await
                        new WebClient().DownloadStringTaskAsync(
                            $@"http://htmlweb.ru/geo/api.php?json&telcod={tel}&api_key={apiKey}");
                var json = JObject.Parse(respJson);

                returnStr.Add(json["0"]["country"].Value<string>());
                returnStr.Add(json["0"]["name"].Value<string>());
                returnStr.Add(json["region"]["name"].Value<string>());
                returnStr.Add(json["okrug"].Value<string>());
                returnStr.Add(json["0"]["oper"].Value<string>());
            }
            catch (Exception ex)
            {
                //Informer.RaiseOnResultReceived(ex);
                Informer.RaiseOnResultReceived($"JSON response for {tel}: {respJson}");
            }
            return returnStr;
        }

        private static async Task MainCyrcle(ISheet sheet)
        {
            for (var num = 1; num <= sheet.LastRowNum; num++)
            {
                try
                {
                    var row = sheet.GetRow(num);
                    if (row == null) //null is when the row only contains empty cells 
                        continue;

                    if (row.GetCell(3).CellType == CellType.Blank)
                        continue;

                    var type = row.GetCell(3).CellType;
                    string tel;

                    switch (type)
                    {
                        case CellType.String:
                            tel = row.GetCell(3).StringCellValue;
                            break;
                        case CellType.Numeric:
                            tel = row.GetCell(3).NumericCellValue.ToString("0000");
                            break;
                        default:
                            throw new Exception($"Wrong phone cell type ({type}) in {num + 1} row");
                    }

                    tel = new string(tel.Where(char.IsDigit).ToArray());

                    if (!Regex.IsMatch(tel, @"^\d{11}$"))
                        continue;

                    if (tel.StartsWith("8"))
                    {
                        tel = "7" + tel.Substring(1); //todo remove
                        row.Cells[3].SetCellValue(tel);
                    }

                    var respStr = await GetCity(tel);
                    if (respStr.Count == 0)
                        continue;

                    var index = 4;
                    foreach (var val in respStr)
                        row.CreateCell(index++).SetCellValue(val);


                    Informer.RaiseOnResultReceived($"{tel}: {respStr}");
                }
                catch (Exception ex)
                {
                    //Informer.RaiseOnResultReceived(ex);
                    Informer.RaiseOnResultReceived($"Is error in {num + 1} row");
                }
            }
        }

        public static async Task ParseXLS(string workBookPath)
        {
            try
            {
                ISheet sheet;
                var fileExt = Path.GetExtension(workBookPath);
                var fileFullPath = Path.GetFullPath(workBookPath);

                if (fileExt != null && fileExt.ToLower() == ".xls")
                {
                    HSSFWorkbook hssfwb;
                    using (var file = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
                        hssfwb = new HSSFWorkbook(file);

                    sheet = hssfwb.GetSheet(hssfwb.GetSheetName(0));
                    await MainCyrcle(sheet);

                    using (var file = new FileStream(fileFullPath, FileMode.Create))
                        hssfwb.Write(file);
                }
                else if (fileExt != null && fileExt.ToLower() == ".xlsx")
                {
                    XSSFWorkbook xssfwb;
                    using (var file = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
                        xssfwb = new XSSFWorkbook(file);

                    sheet = xssfwb.GetSheet(xssfwb.GetSheetName(0));
                    await MainCyrcle(sheet);

                    using (var file = new FileStream(fileFullPath, FileMode.Create))
                        xssfwb.Write(file);
                }

                Informer.RaiseOnResultReceived($"{workBookPath} successfully saved");
            }
            catch (Exception ex)
            {
                Informer.RaiseOnResultReceived(ex);
            }
        }
    }
}