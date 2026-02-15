using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace CRM.EndPoint.FrontEnd.Services {
    public class FileSaver {
        private readonly IWebHostEnvironment appEnvironment;
        private readonly DateTimeServices dtService;

        public FileSaver(IWebHostEnvironment appEnvironment, DateTimeServices dtService)
        {
            this.appEnvironment = appEnvironment;
            this.dtService = dtService;
        }


        public byte[] CompressImage(IFormFile file, long maxBytes = 128 * 1024)
        {
            using (var image = Image.FromStream(file.OpenReadStream()))
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                long quality = 90L;
                byte[] imageBytes;

                do
                {
                    using (var ms = new MemoryStream())
                    {
                        EncoderParameters encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                        image.Save(ms, jpgEncoder, encoderParams);
                        imageBytes = ms.ToArray();
                    }

                    quality -= 10;
                    if (quality < 10) break;
                } while (imageBytes.Length > maxBytes);

                return imageBytes;
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders()
                .FirstOrDefault(codec => codec.FormatID == format.Guid);
        }


        public string GetStringImage(byte[] img)
        {
            if (img != null)
            {
                string imgSt = Convert.ToBase64String(img, 0, img.Length);
                imgSt = "data:image/png;base64," + imgSt;

                return imgSt;
            }

            return null;
        }

        //public byte[] imageToByteArray(Image imageIn)
        //{
        //    MemoryStream ms = new MemoryStream();
        //    imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
        //    return ms.ToArray();
        //}
        public byte[] ConvertStringToByte(string Str)
        {
            var data = Encoding.ASCII.GetBytes(Str);
            return data;
        }

        public IList<Attachment> saveAttach(IList<Attachment> list, _AttachType type)
        {
            foreach (var item in list)
            {
                if (item.Id == Guid.Empty)
                {
                    item.Filename = type.ToString() + "_" + Guid.NewGuid().ToString().Substring(0, 8);
                    item.Url = CreateFileAtt(item.File, item.Filename, item.fileExten);
                    item.CreateDate = DateTime.Now;
                    item.CreateDateTxt = dtService.GetDatePersian();
                    item._AttachType = type;
                    item.AttachDate = DateTime.Now;
                    item.AttachDateTxt = dtService.GetDateTimePersian();
                    item.File = null;
                } else if (item.IsDeleted)
                {
                    DelFile(item.Url);
                }
            }

            return list;
        }


        private string CreateFileAtt(byte[] StrImage, string FileName, string Filetype)
        {
            string fileName = FileName + "." + Filetype;
            CheckUrl(appEnvironment.WebRootPath.ToString() + @"\Uploaded\ContractAtt");

            var filePath = appEnvironment.WebRootPath.ToString() + @"\Uploaded\ContractAtt\" + fileName;

            File.WriteAllBytes(filePath, StrImage);
            return @"\Uploaded\ContractAtt\" + fileName;
        }


        public string SaveAcceptedPreFactorImg(byte[] StrImage, string FileName, string Filetype)
        {
            string fileName = FileName + "." + Filetype;
            CheckUrl(appEnvironment.WebRootPath.ToString() + @"\Uploaded\AcceptedPreFactorImg");

            var filePath = appEnvironment.WebRootPath.ToString() + @"\Uploaded\AcceptedPreFactorImg\" + fileName;
            //byte[] bytes = Convert.FromBase64String(StrImage);
            //var ddd = StrImage.ToString();
            File.WriteAllBytes(filePath, StrImage);
            return @"\Uploaded\AcceptedPreFactorImg\" + fileName;
        }

        public string SaveStringDocFile(byte[] StrImage, string FileName, string Filetype)
        {
            string fileName = FileName + "." + Filetype;
            CheckUrl(appEnvironment.WebRootPath.ToString() + @"\Uploaded\DocFile");

            var filePath = appEnvironment.WebRootPath.ToString() + @"\Uploaded\DocFile\" + fileName;
            //byte[] bytes = Convert.FromBase64String(StrImage);
            //var ddd = StrImage.ToString();
            File.WriteAllBytes(filePath, StrImage);
            return @"\Uploaded\DocFile\" + fileName;
        }


        //public Image byteArrayToImage(byte[] byteArrayIn)
        //{
        //    //byte[] bytes = Convert.FromBase64String(s);
        //    // MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);

        //    // ms.Write(imageBytes, 0, imageBytes.Length);
        //    // Image image = Image.FromStream(ms, true, true);

        //    // return image;


        //    //Image image;
        //    //using (MemoryStream ms = new MemoryStream(byteArrayIn))
        //    //{
        //    //    image = Image.FromStream(ms);

        //    //}

        //    //return image;

        //    ////MemoryStream ms = new MemoryStream(byteArrayIn);
        //    ////
        //    ////Image returnImage = Image.FromStream(ms);

        //    ImageConverter image = new ImageConverter();
        //    Image img = (Bitmap)image.ConvertFrom(byteArrayIn);

        //    return img;
        //}


        //public Image DrawText(string text, Font font, Color textColor, Color backColor)
        //{
        //    //first, create a dummy bitmap just to get a graphics object
        //    Image img = new Bitmap(2, 2);
        //    Graphics drawing = Graphics.FromImage(img);

        //    //measure the string to see how big the image needs to be
        //    SizeF textSize = drawing.MeasureString(text, font);

        //    //free up the dummy image and old graphics object
        //    img.Dispose();
        //    drawing.Dispose();

        //    //create a new image of the right size
        //    img = new Bitmap((int)textSize.Width, (int)textSize.Height);

        //    drawing = Graphics.FromImage(img);

        //    //paint the background
        //    drawing.Clear(backColor);

        //    //create a brush for the text
        //    Brush textBrush = new SolidBrush(textColor);

        //    drawing.DrawString(text, font, textBrush, 2, 2);

        //    drawing.Save();

        //    textBrush.Dispose();
        //    drawing.Dispose();

        //    return img;

        //}

        public string SaveVideoSubExercise(IFormFile formFile)
        {
            if (formFile != null && formFile.Length > 0)
            {
                var fileExtention = Path.GetExtension(formFile.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtention}";
                CheckUrl(appEnvironment.WebRootPath.ToString() + @"\Uploaded\VideosSubExercide");
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploaded\VideosSubExercide", fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    formFile.CopyTo(fileStream);
                }

                return $"/Uploaded/VideosSubExercide/{fileName}";
            }

            return null;
        }

        public string SaveImageSubExercise(IFormFile formFile)
        {
            if (formFile != null && formFile.Length > 0)
            {
                var fileExtention = Path.GetExtension(formFile.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtention}";
                CheckUrl(appEnvironment.WebRootPath.ToString() + @"\Uploaded\ImagesSubExercide");
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploaded\ImagesSubExercide", fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    formFile.CopyTo(fileStream);
                }

                return $"/Uploaded/ImagesSubExercide/{fileName}";
            }

            return null;
        }

        public string SaveImagePersonLogin(IFormFile formFile, string userid, string oldUrl)
        {
            if (formFile != null && formFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + ".jpg";

                CheckUrl(appEnvironment.WebRootPath.ToString() + @"\Uploaded\UserImg");

                var filePath = appEnvironment.WebRootPath.ToString() + @"\Uploaded\UserImg\" + fileName;
                //var filePath = Path.Combine(appEnvironment.WebRootPath.ToString(), @"\Uploaded\UserImg", fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    formFile.CopyTo(fileStream);
                }

                if (oldUrl != null)
                    if (File.Exists(appEnvironment.WebRootPath.ToString() + oldUrl.Replace('/', '\\')))
                    {
                        File.Delete(appEnvironment.WebRootPath.ToString() + oldUrl.Replace('/', '\\'));
                    }

                return $"/Uploaded/UserImg/{fileName}";
            }

            return null;
        }


        public string SaveBaseItemImage(byte[] StrImage, string FileName, string Filetype)
        {
            if (StrImage != null && StrImage.Length > 0)
            {
                string fileName = FileName + "." + Filetype;
                CheckUrl(appEnvironment.WebRootPath.ToString() + @"\Uploaded\CompanyImage");

                var filePath = appEnvironment.WebRootPath.ToString() + @"\Uploaded\CompanyImage\" + fileName;
                //byte[] bytes = Convert.FromBase64String(StrImage);
                //var ddd = StrImage.ToString();
                File.WriteAllBytes(filePath, StrImage);
                return @"\Uploaded\CompanyImage\" + fileName;
            }

            return null;
        }


        public string SaveImageLogo(IFormFile formFile)
        {
            if (formFile != null && formFile.Length > 0)
            {
                var fileName = "Logo.png";
                CheckUrl(appEnvironment.WebRootPath.ToString() + @"\Uploaded\Image");

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Reports\Image", fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    formFile.CopyTo(fileStream);
                }

                return $"/Reports/Image/{fileName}";
            }

            return null;
        }

        public string SaveImageVisit(IFormFile formFile)
        {
            if (formFile != null && formFile.Length > 0)
            {
                var fileExtention = Path.GetExtension(formFile.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtention}";

                CheckUrl(appEnvironment.WebRootPath.ToString() + @"\Uploaded\VisitImages");
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\Uploaded\VisitImages", fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    formFile.CopyTo(fileStream);
                }

                return $"/Uploaded/VisitImages/{fileName}";
            }

            return null;
        }

        public byte[] IformFileToByteArray(IFormFile UpImagesSportMan)
        {
            if (UpImagesSportMan == null)
            {
                return null;
            }

            var ms = new MemoryStream();
            if (UpImagesSportMan != null)
                UpImagesSportMan.CopyTo(ms);
            return ms.ToArray();
        }

        public void DelFile(string Path)
        {
            if (Path != null && Path != "")
            {
                string fullPath = appEnvironment.WebRootPath + Path.Replace('/', '\\');
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }

        public void CheckUrl(string Path)
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
        }
    }
}