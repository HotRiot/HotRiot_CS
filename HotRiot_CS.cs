// HMAC Authentication Info: http://jokecamp.wordpress.com/2012/10/21/examples-of-creating-base64-hashes-using-hmac-sha256-in-different-languages/
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Specialized;

using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Drawing;

// Disable variable not used warning for exceptions.
#pragma warning disable 0168

namespace HotRiot_CS
{
    public delegate void HTTPRequestProgressDelegate(HTTPProgresss httpProgresss);
    public delegate void HTTPPushRequestDelegate(HRPushServiceResponse hrPushServiceResponse);

    public sealed class HotRiot : defines
    {
        private PutDocumentCredentials putDocumentCredentials;
        private static HotRiot HRInstance = new HotRiot();
        private static string PROTOCOL = "https://";
        private static int BUFFER_LENGTH = 4096;

        private string fullyQualifiedHRDAURL;
        private string fullyQualifiedHRURL;
        private Hashtable fileFiledInfo;
        private string jSessionID;
        private string hmKey;

        private HotRiot() { }

        internal static HotRiot getHotRiotInstance
        {
            get
            {
                return HRInstance;
            }
        }

        internal async Task<HotRiotJSON> postLink(string link)
        {
            HotRiotJSON jsonResponse = null;
            WebResponse webResponse = null;
            Stream requestStream = null;
            StreamReader reader = null;
            Stream stream = null;

            try
            {
                int offset = link.IndexOf("?");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link.Substring(0, offset) + jSessionID);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                string postData = link.Substring(offset + 1);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(postData);
                request.ContentLength = bytes.Length;

                requestStream = await request.GetRequestStreamAsync();
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Dispose();
                requestStream.Close();
                requestStream = null;

                webResponse = await request.GetResponseAsync();
                stream = webResponse.GetResponseStream();
                reader = new StreamReader(stream);
                jsonResponse = processResponse(reader.ReadToEnd());
            }

            catch (WebException ex)
            {
                throw new HotRiotException("WebException", ex);
            }
            catch (ArgumentNullException ex)
            {
                throw new HotRiotException("ArgumentNullException", ex);
            }
            catch (OutOfMemoryException ex)
            {
                throw new HotRiotException("OutOfMemoryException", ex);
            }
            catch (IOException ex)
            {
                throw new HotRiotException("IOException", ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new HotRiotException("ArgumentOutOfRangeException", ex);
            }
            catch (AggregateException ex)
            {
                throw new HotRiotException("AggregateException", ex);
            }
            catch (Exception ex)
            {
                throw new HotRiotException("Exception", ex);
            }
            finally
            {
                if (requestStream != null)
                {
                    requestStream.Dispose();
                    requestStream.Close();
                }
                if (reader != null)
                {
                    reader.Dispose();
                    reader.Close();
                }
                if (stream != null)
                {
                    stream.Dispose();
                    stream.Close();
                }
                if (webResponse != null)
                {
                    webResponse.Dispose();
                    webResponse.Close();
                }
            }

            return jsonResponse;
        }

        internal async Task<HotRiotJSON> postRequest(PostRequestParam prp)
        {
            HotRiotJSON jsonResponse = null;
            WebResponse webResponse = null;
            FileStream fileStream = null;
            Stream requestStream = null;
            StreamReader reader = null;
            Stream stream = null;

            try
            {
                string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(prp.url + jSessionID);
                httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                httpWebRequest.Method = "POST";
                httpWebRequest.KeepAlive = true;
                httpWebRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;

                byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";

                requestStream = await httpWebRequest.GetRequestStreamAsync();
                foreach (string key in prp.nvc.Keys)
                {
                    requestStream.Write(boundarybytes, 0, boundarybytes.Length);
                    string formitem = string.Format(formdataTemplate, key, prp.nvc[key]);
                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                    requestStream.Write(formitembytes, 0, formitembytes.Length);
                }

                if (prp.files != null)
                {
                    if (putDocumentCredentials != null)
                    {
                        ArrayList putObjectRequests = putObjectDirect(prp);

                        // This call runs asynchronous with this method. T0
                        // execute synchronous, apply the "await" operator.
                        putObjectDirectS3(putObjectRequests);

                        foreach (string key in prp.files.Keys)
                        {
                            requestStream.Write(boundarybytes, 0, boundarybytes.Length);
                            string formitem = string.Format(formdataTemplate, key, "hsp-sharedfile=" + prp.files[key]);
                            byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                            requestStream.Write(formitembytes, 0, formitembytes.Length);
                        }
                        requestStream.Write(boundarybytes, 0, boundarybytes.Length);

                    }
                    else
                    {
                        string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";
                        requestStream.Write(boundarybytes, 0, boundarybytes.Length);
                        byte[] buffer = new byte[BUFFER_LENGTH];

                        foreach (string key in prp.files.Keys)
                        {
                            string header = string.Format(headerTemplate, key, prp.files[key]);
                            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                            requestStream.Write(headerbytes, 0, headerbytes.Length);

                            fileStream = new FileStream(prp.files[key], FileMode.Open, FileAccess.Read);
                            int bytesRead = 0;
                            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                                requestStream.Write(buffer, 0, bytesRead);
                            boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                            requestStream.Write(boundarybytes, 0, boundarybytes.Length);

                            fileStream.Dispose();
                            fileStream.Close();
                            fileStream = null;
                        }
                    }
                }
                else
                    requestStream.Write(boundarybytes, 0, boundarybytes.Length);

                requestStream.Dispose();
                requestStream.Close();
                requestStream = null;

                webResponse = await httpWebRequest.GetResponseAsync();
                stream = webResponse.GetResponseStream();
                reader = new StreamReader(stream);
                jsonResponse = processResponse(reader.ReadToEnd());
            }
            catch (WebException ex)
            {
                throw new HotRiotException("WebException", ex);
            }
            catch (ArgumentNullException ex)
            {
                throw new HotRiotException("ArgumentNullException", ex);
            }
            catch (OutOfMemoryException ex)
            {
                throw new HotRiotException("OutOfMemoryException", ex);
            }
            catch (ArgumentException ex)
            {
                throw new HotRiotException("ArgumentException", ex);
            }
            catch (FileNotFoundException ex)
            {
                throw new HotRiotException("FileNotFoundException", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new HotRiotException("DirectoryNotFoundException", ex);
            }
            catch (IOException ex)
            {
                throw new HotRiotException("IOException", ex);
            }
            catch (AggregateException ex)
            {
                throw new HotRiotException("AggregateException", ex);
            }
            catch (Exception ex)
            {
                throw new HotRiotException("Exception", ex);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Dispose();
                    fileStream.Close();
                }
                if (requestStream != null)
                {
                    requestStream.Dispose();
                    requestStream.Close();
                }
                if (reader != null)
                {
                    reader.Dispose();
                    reader.Close();
                }
                if (stream != null)
                {
                    stream.Dispose();
                    stream.Close();
                }
                if (webResponse != null)
                {
                    webResponse.Dispose();
                    webResponse.Close();
                }
            }

            return jsonResponse;
        }

        public async Task saveFile(string fileLink, string filePath, HTTPRequestProgressDelegate httpRequestProgressDelegate)
        {
            long bufferLength = BUFFER_LENGTH;

            byte[] response = new byte[bufferLength];
            HTTPProgresss httpProgresss = null;
            WebResponse webResponse = null;
            BinaryReader reader = null;
            FileStream fStream = null;
            Stream stream = null;
            int bytesRead = 0;
            int index = 0;

            try
            {
                if (File.Exists(filePath) == true)
                    throw new IOException("File already exists.");

                if (httpRequestProgressDelegate != null)
                {
                    httpProgresss = new HTTPProgresss();
                    httpProgresss.StartTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fileLink);
                request.Method = "GET";
                webResponse = await request.GetResponseAsync();
                if (webResponse.ContentLength < bufferLength)
                    bufferLength = webResponse.ContentLength;
                stream = webResponse.GetResponseStream();
                if (httpRequestProgressDelegate != null)
                    httpProgresss.TotalBytesToProcess = webResponse.ContentLength;

                fStream = File.Create(filePath);
                reader = new BinaryReader(stream);
                while ((bytesRead = reader.Read(response, 0, (int)bufferLength)) != 0)
                {
                    fStream.Write(response, 0, bytesRead);

                    index += bytesRead;
                    if (webResponse.ContentLength - index < bufferLength)
                        bufferLength = webResponse.ContentLength - index;

                    if (httpRequestProgressDelegate != null)
                    {
                        long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                        httpProgresss.BytesProcessed += bytesRead;
                        httpProgresss.TotalBytesProcessed = index;
                        if (now - httpProgresss.StartTime > httpProgresss.ElapsTimeInMillis + 1000 || httpProgresss.TotalBytesProcessed == httpProgresss.TotalBytesToProcess)
                        {
                            httpProgresss.ElapsTimeInMillis = now - httpProgresss.StartTime;
                            httpRequestProgressDelegate(httpProgresss);
                            httpProgresss.BytesProcessed = 0;
                        }
                    }
                }
            }

            catch (WebException ex)
            {
                throw new HotRiotException("WebException", ex);
            }
            catch (ArgumentNullException ex)
            {
                throw new HotRiotException("ArgumentNullException", ex);
            }
            catch (OutOfMemoryException ex)
            {
                throw new HotRiotException("OutOfMemoryException", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new HotRiotException("DirectoryNotFoundException", ex);
            }
            catch (PathTooLongException ex)
            {
                throw new HotRiotException("PathTooLongException", ex);
            }
            catch (IOException ex)
            {
                throw new HotRiotException("IOException", ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new HotRiotException("ArgumentOutOfRangeException", ex);
            }
            catch (AggregateException ex)
            {
                throw new HotRiotException("AggregateException", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new HotRiotException("UnauthorizedAccessException", ex);
            }
            catch (Exception ex)
            {
                throw new HotRiotException("Exception", ex);
            }
            finally
            {
                if (fStream != null)
                {
                    fStream.Dispose();
                    fStream.Close();
                }
                if (reader != null)
                {
                    reader.Dispose();
                    reader.Close();
                }
                if (stream != null)
                {
                    stream.Dispose();
                    stream.Close();
                }
                if (webResponse != null)
                {
                    webResponse.Dispose();
                    webResponse.Close();
                }
            }
        }

        public async Task<byte[]> readFile(string fileLink, HTTPRequestProgressDelegate httpRequestProgressDelegate)
        {
            HTTPProgresss httpProgresss = null;
            long bufferLength = BUFFER_LENGTH;
            WebResponse webResponse = null;
            BinaryReader reader = null;
            Stream stream = null;
            byte[] response = null;
            int bytesRead = 0;
            int index = 0;

            try
            {
                if (httpRequestProgressDelegate != null)
                {
                    httpProgresss = new HTTPProgresss();
                    httpProgresss.StartTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fileLink);
                request.Method = "GET";
                webResponse = await request.GetResponseAsync();
                if (webResponse.ContentLength < bufferLength)
                    bufferLength = webResponse.ContentLength;
                response = new byte[webResponse.ContentLength];
                stream = webResponse.GetResponseStream();
                if (httpRequestProgressDelegate != null)
                    httpProgresss.TotalBytesToProcess = webResponse.ContentLength;

                reader = new BinaryReader(stream);
                while ((bytesRead = reader.Read(response, index, (int)bufferLength)) != 0)
                {
                    index += bytesRead;
                    if (webResponse.ContentLength - index < bufferLength)
                        bufferLength = webResponse.ContentLength - index;

                    if (httpRequestProgressDelegate != null)
                    {
                        long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                        httpProgresss.BytesProcessed += bytesRead;
                        httpProgresss.TotalBytesProcessed = index;
                        if (now - httpProgresss.StartTime > httpProgresss.ElapsTimeInMillis + 1000 || httpProgresss.TotalBytesProcessed == httpProgresss.TotalBytesToProcess)
                        {
                            httpProgresss.ElapsTimeInMillis = now - httpProgresss.StartTime;
                            httpRequestProgressDelegate(httpProgresss);
                            httpProgresss.BytesProcessed = 0;
                        }
                    }
                }
            }

            catch (WebException ex)
            {
                throw new HotRiotException("WebException", ex);
            }
            catch (ArgumentNullException ex)
            {
                throw new HotRiotException("ArgumentNullException", ex);
            }
            catch (OutOfMemoryException ex)
            {
                throw new HotRiotException("OutOfMemoryException", ex);
            }
            catch (IOException ex)
            {
                throw new HotRiotException("IOException", ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new HotRiotException("ArgumentOutOfRangeException", ex);
            }
            catch (AggregateException ex)
            {
                throw new HotRiotException("AggregateException", ex);
            }
            catch (Exception ex)
            {
                throw new HotRiotException("Exception", ex);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                    reader.Close();
                }
                if (stream != null)
                {
                    stream.Dispose();
                    stream.Close();
                }
                if (webResponse != null)
                {
                    webResponse.Dispose();
                    webResponse.Close();
                }
            }

            return response;
        }

        public async Task<FileMetadata> getFileMetadata(string fileLink)
        {
            FileMetadata fileMetadata = new FileMetadata();
            WebResponse webResponse = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fileLink);
                request.Method = "GET";
                webResponse = await request.GetResponseAsync();
                fileMetadata.ContentLength = webResponse.ContentLength;
                fileMetadata.ContentType = webResponse.ContentType;
                fileMetadata.IsFromCache = webResponse.IsFromCache;

                WebHeaderCollection WebHeadersCollection = webResponse.Headers;
                fileMetadata.Date = WebHeadersCollection.Get("Date");
                fileMetadata.LastModified = WebHeadersCollection.Get("Last-Modified");
            }

            catch (WebException ex)
            {
                throw new HotRiotException("WebException", ex);
            }
            catch (ArgumentNullException ex)
            {
                throw new HotRiotException("ArgumentNullException", ex);
            }
            catch (OutOfMemoryException ex)
            {
                throw new HotRiotException("OutOfMemoryException", ex);
            }
            catch (IOException ex)
            {
                throw new HotRiotException("IOException", ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new HotRiotException("ArgumentOutOfRangeException", ex);
            }
            catch (AggregateException ex)
            {
                throw new HotRiotException("AggregateException", ex);
            }
            catch (Exception ex)
            {
                throw new HotRiotException("Exception", ex);
            }
            finally
            {
                if (webResponse != null)
                {
                    webResponse.Dispose();
                    webResponse.Close();
                }
            }

            return fileMetadata;
        }

        private string HMACToken(string message)
        {
            string base64Message = null;

            if (hmKey != null)
            {
                var encoding = new System.Text.UTF8Encoding();
                byte[] keyByte = encoding.GetBytes(hmKey);
                byte[] messageBytes = encoding.GetBytes(message);
                using (var hmacsha256 = new HMACSHA256(keyByte))
                {
                    byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                    base64Message = Convert.ToBase64String(hashmessage);
                }
            }

            return base64Message;
        }

        private ArrayList putObjectDirect(PostRequestParam prp)
        {
            ArrayList putObjectRequests = new ArrayList();
            string[] allKeys = prp.files.AllKeys;

            foreach (string key in allKeys)
            {
                bool process = true;

                try
                {
                    if (fileFiledInfo != null)
                    {
                        long fileSizeLimit = (long)fileFiledInfo[prp.databaseName + key];
                        if (fileSizeLimit != null)
                            if (new FileInfo(prp.files[key]).Length > fileSizeLimit)
                                process = false;
                    }
                }
                catch (Exception doNothing) { }

                if (process == true)
                {
                    string filename = helpers.GetUniqueKey(28) + "-" + Path.GetFileName(prp.files[key]);
                    PutObjectRequestLocal putObjectRequestLocal = new PutObjectRequestLocal
                    {
                        BucketName = putDocumentCredentials.bucket,
                        Key = putDocumentCredentials.key + filename,
                        FilePath = prp.files[key]
                    };

                    putObjectRequests.Add(putObjectRequestLocal);
                    prp.files[key] = filename;
                }
            }

            return putObjectRequests;
        }

        private async Task putObjectDirectS3(ArrayList putObjectRequestsLocal)
        {
            ArrayList putObjectRequests = new ArrayList();

            foreach (PutObjectRequestLocal putObjectRequestLocal in putObjectRequestsLocal)
            {
                try
                {
                    using (FileStream fs = new FileStream(putObjectRequestLocal.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        S3.putFile(putDocumentCredentials.aKey, putDocumentCredentials.sKey, fs, putObjectRequestLocal.Key, putObjectRequestLocal.BucketName, putDocumentCredentials.sessionToken);
                    }
                }
                catch (Exception doNothing) { }
            }

            foreach (PutObjectRequestLocal putObjectRequestLocal in putObjectRequestsLocal)
            {
                try
                {
                    string extension = Path.GetExtension(putObjectRequestLocal.Key);
                    if (extension != null && (extension.Equals(".jpg") == true || extension.Equals(".jpeg") == true || extension.Equals(".jpe") == true))
                    {
                        Bitmap resized = null;
                        double scalefactor;

                        // Below is a less efficient method for generating thumbnails. However, this is compatable with
                        // mono. See the commented code block below for a more efficient method for thumbnail generation.
                        try
                        {
                            using (FileStream fs = new FileStream(putObjectRequestLocal.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                using (Bitmap origBitmap = new Bitmap(fs))
                                {
                                    if (origBitmap.Width > origBitmap.Height)
                                        scalefactor = putDocumentCredentials.thumbnailSize / (double)origBitmap.Width;
                                    else
                                        scalefactor = putDocumentCredentials.thumbnailSize / (double)origBitmap.Height;

                                    resized = new Bitmap((int)(origBitmap.Width * scalefactor), (int)(origBitmap.Height * scalefactor));
                                    using (Graphics gr = Graphics.FromImage(resized))
                                    {
                                        gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                        gr.DrawImage(origBitmap, new Rectangle(0, 0, (int)(origBitmap.Width * scalefactor), (int)(origBitmap.Height * scalefactor)));
                                    }
                                }
                            }

                            int indexPos = putObjectRequestLocal.Key.LastIndexOf("/");
                            if (indexPos == -1)
                                putObjectRequestLocal.Key = "thumbnails/" + putObjectRequestLocal.Key;
                            else
                                putObjectRequestLocal.Key = putObjectRequestLocal.Key.Insert(indexPos + 1, "thumbnails/");
                            putObjectRequestLocal.FilePath = null;

                            using (MemoryStream ms = new MemoryStream())
                            {
                                System.Drawing.Imaging.EncoderParameters EncoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
                                EncoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
                                resized.Save(ms, GetEncoderInfo("image/jpeg"), EncoderParameters);
                                S3.putFile(putDocumentCredentials.aKey, putDocumentCredentials.sKey, ms, putObjectRequestLocal.Key, putObjectRequestLocal.BucketName, putDocumentCredentials.sessionToken);
                            }
                        }
                        finally
                        {
                            if (resized != null)
                                resized.Dispose();
                        }
                        // End less efficient thumbnail generation.


                        // Below is more efficient method for generating thumbnails. Currently, not compatable with
                        // mono. If you wish to use this method for creating thumbnails, uncomment the code block below,
                        // then comment out the less efficient method above. Finally, add the WindowsBase assembly and the 
                        // PresentationCore assembly into your project.
                        /* 
                        System.Windows.Media.Imaging.BitmapImage resizedImage = new System.Windows.Media.Imaging.BitmapImage();
                        int originWidth;
                        int originHeight;

                        // Open a Stream to get JPEG image dimensions.
                        using (Stream imageStreamSource = new FileStream(putObjectRequestLocal.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            System.Windows.Media.Imaging.BitmapDecoder decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(imageStreamSource, System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.None);
                            System.Windows.Media.Imaging.BitmapFrame frame = decoder.Frames[0];
                            originHeight = frame.PixelHeight;
                            originWidth = frame.PixelWidth;
                        }

                        if (originWidth > originHeight)
                            scalefactor = putDocumentCredentials.thumbnailSize / (double)originWidth;
                        else
                            scalefactor = putDocumentCredentials.thumbnailSize / (double)originHeight;

                        // Open a Stream and decode a JPEG thumbnail image.
                        using (Stream imageStreamSource = new FileStream(putObjectRequestLocal.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            resizedImage.BeginInit();
                            resizedImage.StreamSource = imageStreamSource;
                            resizedImage.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreColorProfile;
                            resizedImage.DecodePixelHeight = (int)(originHeight * scalefactor);
                            resizedImage.DecodePixelWidth = (int)(originWidth * scalefactor);

                            // This does the actual loading and resizing. This is very efficient because the codec
                            // will scale while decoding, rendering only those pixels that will be in the thumbnail.
                            resizedImage.EndInit();    

                            System.Windows.Media.Imaging.JpegBitmapEncoder encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder();
                            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(resizedImage));
                            using (MemoryStream ms = new MemoryStream())
                            {
                                encoder.QualityLevel = 100;
                                encoder.Save(ms);
                                int indexPos = putObjectRequestLocal.Key.LastIndexOf("/");
                                if (indexPos == -1)
                                    putObjectRequestLocal.Key = "thumbnails/" + putObjectRequestLocal.Key;
                                else
                                    putObjectRequestLocal.Key = putObjectRequestLocal.Key.Insert(indexPos + 1, "thumbnails/");
                                putObjectRequestLocal.FilePath = null;

                                S3.putFile(putDocumentCredentials.aKey, putDocumentCredentials.sKey, ms, putObjectRequestLocal.Key, putObjectRequestLocal.BucketName, putDocumentCredentials.sessionToken);
                            }
                        }
                        // End more efficient thumbnail generation.
                        */
                    }
                }
                catch (Exception doNothing) { }
            }
        }

        private static System.Drawing.Imaging.ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            System.Drawing.Imaging.ImageCodecInfo[] encoders;
            encoders = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }

            return null;
        }

        private async Task getPutDocumentCredentials()
        {
            if (putDocumentCredentials != null)
                if (putDocumentCredentials.creationTime + (TimeSpan.TicksPerMinute * 14) > DateTime.Now.Ticks)
                    return;

            await submitGetPutCredentialsRequest();
        }

        private async Task submitGetPutCredentialsRequest()
        {
            putDocumentCredentials = null;

            NameValueCollection rollSessionParameters = new NameValueCollection();
            rollSessionParameters.Set("hsp-initializepage", "hsp-rollsessionprovider");
            if (fileFiledInfo == null)
                rollSessionParameters.Set("hsp-getFileFieldInfo", "hsp-getFileFieldInfo");

            HRRollSessionResponse hrRollSessionResponse = new HRRollSessionResponse(await postRequest(new PostRequestParam(fullyQualifiedHRURL, rollSessionParameters)));
            if (hrRollSessionResponse.getResultCode() == 0)
            {
                putDocumentCredentials = new PutDocumentCredentials();

                putDocumentCredentials.aKey = getGeneralInfoString(hrRollSessionResponse, "aKey");
                putDocumentCredentials.sKey = getGeneralInfoString(hrRollSessionResponse, "sKey");
                putDocumentCredentials.key = getGeneralInfoString(hrRollSessionResponse, "key");
                putDocumentCredentials.bucket = getGeneralInfoString(hrRollSessionResponse, "bucket");
                putDocumentCredentials.sessionToken = getGeneralInfoString(hrRollSessionResponse, "sessionToken");
                putDocumentCredentials.thumbnailSize = getGeneralInfoInteger(hrRollSessionResponse, "thumbnailSize");
                putDocumentCredentials.creationTime = DateTime.Now.Ticks;

                if (fileFiledInfo == null)
                    fileFiledInfo = hrRollSessionResponse.getFileFieldInfo();
            }
        }

        private HotRiotJSON processResponse(string unprocessedJsonResponse)
        {
            HotRiotJSON hotriotJSON = new HotRiotJSON(JObject.Parse(unprocessedJsonResponse));
            setSession(hotriotJSON);
            return hotriotJSON;
        }

        private void setSession(HotRiotJSON jsonResponse)
        {
            String sessionID = getGeneralInfoString(jsonResponse, "sessionID");
            if (sessionID != null)
                jSessionID = ";jsessionid=" + sessionID;
        }

        private string getGeneralInfoString(HotRiotJSON jsonResponse, string field)
        {
            try
            {
                return processDataString(jsonResponse["generalInformation"][field].ToString());
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return null;
        }

        private int getGeneralInfoInteger(HotRiotJSON jsonResponse, string field)
        {
            try
            {
                return (int)jsonResponse["generalInformation"][field];
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return 0;
        }

        private string processDataString(string data)
        {
            if (data != null)
                if (data.Length == 0)
                    data = null;

            return data;
        }

        // ------------------------------------ INITIALIZE HOTRIOT ------------------------------------
        public static HotRiot init(string appName)
        {
            HotRiot hotriot = HotRiot.getHotRiotInstance;

            hotriot.fullyQualifiedHRDAURL = PROTOCOL + appName + ".k222.info/da";
            hotriot.fullyQualifiedHRURL = PROTOCOL + appName + ".k222.info/process";

            return hotriot;
        }

        // ----------------------------------- ACTION OPERATIONS ------------------------------------
        public async Task<HRInsertResponse> submitRecord(string databaseName, NameValueCollection recordData, NameValueCollection files)
        {
            if (files != null)
                await getPutDocumentCredentials();

            recordData.Set("hsp-formname", databaseName);
            return new HRInsertResponse(await postRequest(new PostRequestParam(fullyQualifiedHRURL, recordData, files, databaseName)));
        }

        public async Task<HRInsertResponse> submitUpdateRecord(string databaseName, string recordID, string updatePassword, NameValueCollection recordData, NameValueCollection files)
        {
            if (files != null)
                await getPutDocumentCredentials();

            recordData.Set("hsp-formname", databaseName);
            recordData.Set("hsp-json", updatePassword);
            recordData.Set("hsp-recordID", recordID);
            return new HRInsertResponse(await postRequest(new PostRequestParam(fullyQualifiedHRURL, recordData, files, databaseName)));
        }

        public async Task<HRInsertResponse> deleteFile(string databaseName, string recordID, string updatePassword, string fieldName)
        {
            NameValueCollection recordData = new NameValueCollection();

            recordData.Set(fieldName, "hsp-deletefile");
            return await submitUpdateRecord(databaseName, recordID, updatePassword, recordData, null);
        }

        public async Task<HRSearchResponse> submitSearch(string searchName, NameValueCollection searchCriterion)
        {
            searchCriterion.Set("hsp-formname", searchName);
            return new HRSearchResponse(await postRequest(new PostRequestParam(fullyQualifiedHRURL, searchCriterion)));
        }

        public async Task<HRLoginResponse> submitLogin(string loginName, NameValueCollection loginCredentials)
        {
            loginCredentials.Set("hsp-formname", loginName);
            return new HRLoginResponse(await postRequest(new PostRequestParam(fullyQualifiedHRURL, loginCredentials)));
        }

        public async Task<HRNotificationResponse> submitNotification(string databaseName, NameValueCollection notificationData)
        {
            notificationData.Set("hsp-formname", databaseName);
            notificationData.Set("hsp-rtninsert", "1");
            return new HRNotificationResponse(await postRequest(new PostRequestParam(fullyQualifiedHRURL, notificationData)));
        }

        public async Task<HRLoginLookupResponse> submitLostLoginLookup(string loginName, NameValueCollection loginLookupData)
        {
            loginLookupData.Set("hsp-formname", loginName);
            return new HRLoginLookupResponse(await postRequest(new PostRequestParam(fullyQualifiedHRURL, loginLookupData)));
        }

        public async Task<HRRecordCountResponse> submitRecordCount(NameValueCollection recordCountObject)
        {
            return new HRRecordCountResponse(await submitRecordCount(recordCountObject, "false"));
        }

        public async Task<HRRecordCountResponse> submitRecordCountSLL(NameValueCollection recordCountObject)
        {
            return new HRRecordCountResponse(await submitRecordCount(recordCountObject, "true"));
        }

        private async Task<HotRiotJSON> submitRecordCount(NameValueCollection recordCountObject, string sll)
        {
            recordCountObject.Set("hsp-initializepage", "hsp-json");
            recordCountObject.Set("hsp-action", "recordcount");
            recordCountObject.Set("hsp-sll", sll);
            recordCountObject.Set("sinceLastLogin", "false");
            return await postRequest(new PostRequestParam(fullyQualifiedHRURL, recordCountObject));
        }

        public async Task<HRPushServiceResponse> submitPushServiceRequest(NameValueCollection pushParameters, HTTPPushRequestDelegate httpPushRequestDelegate)
        {
            pushParameters.Set("hsp-initializepage", "hsp-mpush");
            HRPushServiceResponse hrPushServiceResponse = new HRPushServiceResponse(await postRequest(new PostRequestParam(fullyQualifiedHRURL, pushParameters)));

            if (httpPushRequestDelegate != null)
                httpPushRequestDelegate(hrPushServiceResponse);

            return hrPushServiceResponse;
        }

        public async Task<HRLogoutResponse> submitLogout(NameValueCollection logoutOptions)
        {
            string callbackData = null;

            if (logoutOptions != null)
                if (logoutOptions["hsp-callbackdata"] != null)
                    callbackData = "&hsp-callbackdata=" + logoutOptions["hsp-callbackdata"];

            return new HRLogoutResponse(await postLink(fullyQualifiedHRDAURL + "?hsp-logout=hsp-json" + callbackData));

        }

        // Helper Method.
        public async Task<HotRiotJSON> deleteRecordDirect(string deleteRecordCommand, bool repost)
        {
            if (repost == false)
                deleteRecordCommand = deleteRecordCommand + "&norepost=true";

            if (repost == true)
                return new HRSearchResponse(await postLink(deleteRecordCommand));
            else
                return new HRDeleteResponse(await postLink(deleteRecordCommand));
        }
    }

    public class HRResponse : HotRiotJSON
    {
        public HRResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }

        private bool isActionValid(string validAction)
        {
            string action = getAction();
            if (action != null && action.Equals(validAction) == true)
                return true;

            return false;
        }

        private string getGeneralInfoString(string field)
        {
            try
            {
                return processDataString(this["generalInformation"][field].ToString());
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return null;
        }

        private string getSubscriptionInfoString(string field)
        {
            try
            {
                return processDataString(this["subscriptionDetails"][field].ToString());
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return null;
        }

        private int getSubscriptionInfoInteger(string field)
        {
            try
            {
                return (int)this["subscriptionDetails"][field];
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return 0;
        }

        private string getSubscriptionPaymentInfoString(string field)
        {
            try
            {
                return processDataString(this["subscriptionPaymentInfo"][field].ToString());
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return null;
        }

        private int getSubscriptionPaymentInfoInteger(string field)
        {
            try
            {
                return (int)this["subscriptionPaymentInfo"][field];
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return 0;
        }

        private bool getGeneralInfoBool(string field)
        {
            try
            {
                return (bool)this["generalInformation"][field];
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return false;
        }

        private int getGeneralInfoInteger(string field)
        {
            try
            {
                return (int)this["generalInformation"][field];
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return 0;
        }

        private long getGeneralInfoLong(string field)
        {
            try
            {
                return (long)this["generalInformation"][field];
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return 0;
        }

        private string[] getGeneralInfoArray(string field)
        {
            string[] retArray = null;

            try
            {
                string jsonField = getGeneralInfoString(field);
                if (jsonField != null)
                {
                    JArray fieldJArray = JArray.Parse(jsonField);

                    retArray = new string[fieldJArray.Count];
                    for (int i = 0; i < fieldJArray.Count; i++)
                        retArray[i] = (String)fieldJArray[i];
                }
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }
            catch (Exception doNothing) { }

            return retArray;
        }

        private string[] getGeneralInfoArray(string field, int index)
        {
            string[] retArray = null;

            try
            {
                string jsonField = getGeneralInfoString(field);
                if (jsonField != null)
                {
                    JArray fieldJArray = JArray.Parse(jsonField);
                    fieldJArray = JArray.Parse(fieldJArray[index].ToString());

                    retArray = new string[fieldJArray.Count];
                    for (int i = 0; i < fieldJArray.Count; i++)
                        retArray[i] = (String)fieldJArray[i];
                }
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }
            catch (Exception doNothing) { }

            return retArray;
        }

        private bool isValidRecordNumber(int recordNumber)
        {
            if (recordNumber > 0)
                if (recordNumber <= getGeneralInfoInteger("recordCount"))
                    return true;

            return false;
        }

        private string getFieldDataString(int recordNumber, string dbFieldName)
        {
            try
            {
                string finalRecordNumber = "record_" + recordNumber;
                return processDataString(this["recordData"][finalRecordNumber]["fieldData"][dbFieldName].ToString());
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return null;
        }

        private string getRecordDataString(int recordNumber, string recordDataName)
        {
            try
            {
                string finalRecordNumber = "record_" + recordNumber;
                return processDataString(this["recordData"][finalRecordNumber][recordDataName].ToString());
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return null;
        }

        private string getSubscriptionPaymentInfoString(int recordNumber, string fieldName)
        {
            try
            {
                string finalRecordNumber = "payment_" + recordNumber;
                return processDataString(this["subscriptionPaymentInfo"][finalRecordNumber][fieldName].ToString());
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }

            return null;
        }

        private string processDataString(string data)
        {
            if (data != null)
                if (data.Length == 0)
                    data = null;

            return data;
        }

        private FieldInfo getDatabaseFieldInfo(int recordNumber, string fieldName, string databaseName)
        {
            string dbFieldName = databaseName + "::" + fieldName;

            string jFieldInfoString = null;
            FieldInfo recordInfo = null;
            if ((jFieldInfoString = getFieldDataString(recordNumber, dbFieldName)) != null)
            {
                JObject jFieldInfo = JObject.Parse(jFieldInfoString);
                recordInfo = new FieldInfo();
                recordInfo.DataCount = (int)jFieldInfo["dataCount"];
                recordInfo.DataType = (string)jFieldInfo["dataType"];
                recordInfo.FieldName = fieldName;
                recordInfo.DatabaseName = databaseName;

                if (recordInfo.DataCount != 0)
                {
                    JArray valueString = (JArray)jFieldInfo["value"];
                    recordInfo.allocateFieldData(valueString.Count);
                    for (int i = 0; i < valueString.Count; i++)
                        recordInfo[i] = (String)valueString[i];

                    recordInfo.SortLink = (string)jFieldInfo["sortLink"];
                    if (recordInfo.DataType == "File")
                    {
                        recordInfo.FileLinkURL = (string)jFieldInfo["fileLinkURL"];
                        if ((recordInfo.IsPicture = isImage(recordInfo[0])) == true)
                            recordInfo.ThumbnailLinkURL = (string)jFieldInfo["thumbnailLinkURL"];
                    }
                    else
                        recordInfo.IsPicture = false;
                }
            }

            return recordInfo;
        }

        private bool isImage(string filename)
        {
            string[] parts = filename.Split('.');
            if (parts.Length > 1)
            {
                string extension = parts[parts.Length - 1].ToLower();
                if (extension.Equals("jpg") == true || extension.Equals("jpeg") == true)
                    return true;
            }

            return false;
        }

        private string getJoinRecordSystemFieldData(int recordNumber, string systemFieldName, string databaseName)
        {
            string fieldData = null;

            string dbFieldName = databaseName + "::" + systemFieldName;

            if (isValidRecordNumber(recordNumber) == true)
                fieldData = getFieldDataString(recordNumber, dbFieldName);

            return fieldData;
        }

        private DatabaseRecord getTriggerRecordInfo(int recordNumber, string triggerDatabaseName)
        {
            DatabaseRecord databaseRecord = null;

            if (isValidRecordNumber(recordNumber) == true)
            {
                var triggerDatabaseFieldNames = getTriggerFieldNames(triggerDatabaseName);
                if (triggerDatabaseFieldNames != null && triggerDatabaseFieldNames.Length > 0)
                {
                    databaseRecord = new DatabaseRecord(triggerDatabaseFieldNames.Length);

                    for (int i = 0; i < triggerDatabaseFieldNames.Length; i++)
                        databaseRecord.add(getDatabaseFieldInfo(recordNumber, triggerDatabaseFieldNames[i], triggerDatabaseName));
                }
            }

            return databaseRecord;
        }

        /********************************************* PUBLIC API *********************************************/

        // ------------------------------------- CHECKING RESULTS -------------------------------------
        public int getResultCode()
        {
            return getGeneralInfoInteger("processingResultCode");
        }

        public string getResultText()
        {
            return getGeneralInfoString("processingResult");
        }

        public string getResultMessage()
        {
            return getGeneralInfoString("processingResultMessage");
        }

        public ResultDetails getResultDetails()
        {
            ResultDetails resultDetails = new ResultDetails();

            resultDetails.ResultCode = getResultCode();
            resultDetails.ResultText = getResultText();
            resultDetails.ResultMessage = getResultMessage();
            resultDetails.ProcessingTimeStamp = getGeneralInfoString("timeStamp");

            return resultDetails;
        }

        // ------------------------------------- GETTING ACTION -------------------------------------
        public string getAction()
        {
            return getGeneralInfoString("action");
        }

        // ------------------------------------- INSERT ACTION -------------------------------------
        public bool isUpdate()
        {
            return getGeneralInfoBool("isUpdate");
        }

        public string getInsertDatabaseName()
        {
            return getDatabaseName();
        }

        public string[] getInsertFieldNames()
        {
            return getFieldNames();
        }

        public DatabaseRecord getInsertData()
        {
            return getRecord(1);
        }

        public async Task<HRUserDataResponse> getUserInfo()
        {
            String loggedInUserInfoLink = getGeneralInfoString("loggedInUserInfoLink");

            if (loggedInUserInfoLink != null)
                return new HRUserDataResponse(await HotRiot.getHotRiotInstance.postLink(loggedInUserInfoLink));

            return null;
        }

        public long getDatePosted()
        {
            return getGeneralInfoLong("datePosted");
        }

        public string getCallbackData()
        {
            return getGeneralInfoString("userData");
        }

        // ------------------------------------- SEARCH ACTION -------------------------------------
        public string getSearchName()
        {
            return getGeneralInfoString("searchName");
        }

        public RecordCountDetails getRecordCountInfo()
        {
            RecordCountDetails recordCountDetails = new RecordCountDetails();

            recordCountDetails.RecordCount = getGeneralInfoInteger("recordCount");
            recordCountDetails.PageCount = getGeneralInfoInteger("pageCount");
            recordCountDetails.PageNumber = getGeneralInfoInteger("pageNumber");
            recordCountDetails.TotalRecordsFound = getGeneralInfoInteger("totalRecordsFound");

            return recordCountDetails;
        }

        public string getDatabaseName()
        {
            return getGeneralInfoString("databaseName");
        }

        public string[] getJoinDatabaseNames()
        {
            return getGeneralInfoArray("join");
        }

        public string[] getFieldNames()
        {
            return getGeneralInfoArray("databaseFieldNames");
        }

        public string[] getJoinFieldNames(string joinDatabaseName)
        {
            string[] joinFieldNames = null;
            string[] joinDatabaseNames = getJoinDatabaseNames();

            if (joinDatabaseNames != null)
                for (var i = 0; i < joinDatabaseNames.Length; i++)
                    if (joinDatabaseNames[i] == joinDatabaseName)
                    {
                        joinFieldNames = getGeneralInfoArray("joinFieldNames", i);
                        break;
                    }

            return joinFieldNames;
        }

        public DatabaseRecord getRecord(int recordNumber)
        {
            DatabaseRecord databaseRecord = null;

            if (isValidRecordNumber(recordNumber) == true)
            {
                string databaseName = getDatabaseName();
                string[] databaseFieldNames = getFieldNames();

                if (databaseFieldNames != null && databaseName != null)
                {
                    if (databaseFieldNames.Length > 0)
                        databaseRecord = new DatabaseRecord(databaseFieldNames.Length);

                    for (var i = 0; i < databaseFieldNames.Length; i++)
                        databaseRecord.add(getDatabaseFieldInfo(recordNumber, databaseFieldNames[i], databaseName));
                }
            }

            return databaseRecord;
        }

        public DatabaseRecord getJoinRecord(int recordNumber, string joinDatabaseName)
        {
            DatabaseRecord databaseRecord = null;

            if (isValidRecordNumber(recordNumber) == true)
            {
                string[] joinDatabaseFieldNames = getJoinFieldNames(joinDatabaseName);
                if (joinDatabaseFieldNames.Length > 0)
                {
                    databaseRecord = new DatabaseRecord(joinDatabaseFieldNames.Length);

                    for (var i = 0; i < joinDatabaseFieldNames.Length; i++)
                        databaseRecord.add(getDatabaseFieldInfo(recordNumber, joinDatabaseFieldNames[i], joinDatabaseName));
                }
            }

            return databaseRecord;
        }

        public async Task<HRGetTriggerResponse> getTriggerRecords(int recordNumber)
        {
            HRGetTriggerResponse jsonRecordDetailsResponse = null;

            if (isValidRecordNumber(recordNumber) == true)
            {
                string recordLink = getRecordDataString(recordNumber, "recordLink");
                if (recordLink != null)
                    jsonRecordDetailsResponse = new HRGetTriggerResponse(await HotRiot.getHotRiotInstance.postLink(recordLink));
            }

            return jsonRecordDetailsResponse;
        }

        public async Task<HRSearchResponse> sortSearchResults(string fieldName)
        {
            return await sortSearchResultsEx(null, fieldName);
        }

        public async Task<HRSearchResponse> sortSearchResultsEx(string databaseName, string fieldName)
        {
            FieldInfo recordInfo;

            if (databaseName == null)
            {
                databaseName = getDatabaseName();
                recordInfo = getDatabaseFieldInfo(1, fieldName, databaseName);

                // If I could not find the fieldname in the primary database, chack to see if it exists in any joined databases.
                if (recordInfo == null)
                {
                    string[] joinDatabaseNames = getJoinDatabaseNames();
                    if (joinDatabaseNames != null)
                        for (var i = 0; i < joinDatabaseNames.Length; i++)
                        {
                            recordInfo = getDatabaseFieldInfo(1, fieldName, joinDatabaseNames[i]);
                            if (recordInfo != null)
                                break;
                        }
                }

                // If I could not find the fieldname in the primary database or any of the joined databases, chack to see if it exists in any trigger databases.
                if (recordInfo == null)
                {
                    string[] triggerDatabaseNames = getTriggerDatabaseNames();
                    if (triggerDatabaseNames != null)
                        for (var x = 0; x < triggerDatabaseNames.Length; x++)
                        {
                            recordInfo = getDatabaseFieldInfo(1, fieldName, triggerDatabaseNames[x]);
                            if (recordInfo != null)
                                break;
                        }
                }

                // If a record was found with the fieldName, then post the sort link.
                if (recordInfo != null)
                    return new HRSearchResponse(await HotRiot.getHotRiotInstance.postLink(recordInfo.SortLink));
            }
            else
            {
                recordInfo = getDatabaseFieldInfo(1, fieldName, databaseName);
                if (recordInfo != null)
                    new HRSearchResponse(await HotRiot.getHotRiotInstance.postLink(recordInfo.SortLink));
            }

            return null;
        }

        public async Task<HRSearchResponse> getNextPage()
        {
            string nextPageLink = getGeneralInfoString("nextPageLinkURL");
            if (nextPageLink != null)
                return new HRSearchResponse(await HotRiot.getHotRiotInstance.postLink(nextPageLink));

            return null;
        }

        public async Task<HRSearchResponse> getPreviousPage()
        {
            string nextPageLink = getGeneralInfoString("previousPageLinkURL");
            if (nextPageLink != null)
                return new HRSearchResponse(await HotRiot.getHotRiotInstance.postLink(nextPageLink));

            return null;
        }

        public async Task<HRSearchResponse> getFirstPage()
        {
            string nextPageLink = getGeneralInfoString("firstPageLinkURL");
            if (nextPageLink != null)
                return new HRSearchResponse(await HotRiot.getHotRiotInstance.postLink(nextPageLink));

            return null;
        }

        public bool moreRecords()
        {
            int pageCount = getGeneralInfoInteger("pageCount");
            int pageNumber = getGeneralInfoInteger("pageNumber");

            if (pageNumber != 0 && pageCount != 0 && pageNumber < pageCount)
                return true;

            return false;
        }

        // public bool getUserInfo(HotRiotJSON jsonResponse) Implementation in Insert Action

        public string getDeleteRecordCommand(int recordNumber)
        {
            if (isValidRecordNumber(recordNumber) == true)
                return getRecordDataString(recordNumber, "deleteRecordLink");

            return null;
        }

        public string getJoinDeleteRecordCommand(int recordNumber, string joinDatabaseName)
        {
            return getJoinRecordSystemFieldData(recordNumber, "hsp-deleteRecordLink", joinDatabaseName);
        }

        public async Task<HotRiotJSON> deleteRecord(int recordNumber, bool repost)
        {
            if (isValidRecordNumber(recordNumber) == true)
            {
                string deleteRecordCommand = getDeleteRecordCommand(recordNumber);
                if (deleteRecordCommand != null)
                    return await deleteRecordDirect(deleteRecordCommand, repost);
                else
                    throw new HotRiotException("Unauthorized Access.");
            }

            return null;
        }

        public async Task<HotRiotJSON> deleteFile(int recordNumber, string fieldName)
        {
            if (isValidRecordNumber(recordNumber) == true)
            {
                string recordID = getRecordID(recordNumber);
                string updatePassword = getEditRecordPassword(recordNumber);
                string databaseName = getDatabaseName();

                if (recordID != null && updatePassword != null && databaseName != null)
                    return await HotRiot.getHotRiotInstance.deleteFile(databaseName, recordID, updatePassword, fieldName);
                else
                    throw new HotRiotException("Unauthorized Access.");
            }

            return null;
        }

        public async Task<HotRiotJSON> deleteJoinRecord(int recordNumber, string joinDatabaseName, bool repost)
        {
            if (isValidRecordNumber(recordNumber) == true)
            {
                string deleteRecordCommand = getJoinDeleteRecordCommand(recordNumber, joinDatabaseName);
                if (deleteRecordCommand != null)
                    return await deleteRecordDirect(deleteRecordCommand, repost);
                else
                    throw new HotRiotException("Unauthorized Access.");
            }

            return null;
        }

        public async Task<HotRiotJSON> deleteRecordDirect(string deleteRecordCommand, bool repost)
        {
            if (repost == false)
                deleteRecordCommand = deleteRecordCommand + "&norepost=true";

            if (repost == true)
                return new HRSearchResponse(await HotRiot.getHotRiotInstance.postLink(deleteRecordCommand));
            else
                return new HRDeleteResponse(await HotRiot.getHotRiotInstance.postLink(deleteRecordCommand));
        }

        public string getEditRecordPassword(int recordNumber)
        {
            if (isValidRecordNumber(recordNumber) == true)
                return getRecordDataString(recordNumber, "editRecordPswd");

            return null;
        }

        public string getJoinEditRecordPassword(int recordNumber, string joinDatabaseName)
        {
            return getJoinRecordSystemFieldData(recordNumber, "hsp-editRecordPswd", joinDatabaseName);
        }

        public string getRecordID(int recordNumber)
        {
            if (isValidRecordNumber(recordNumber) == true)
                return getRecordDataString(recordNumber, "recordID");

            return null;
        }

        public string getJoinRecordID(int recordNumber, string joinDatabaseName)
        {
            return getJoinRecordSystemFieldData(recordNumber, "hsp-recordID", joinDatabaseName);
        }

        public HotRiotJSON getJsonResponseFromRSL(string fieldName)
        {
            return null;
        }

        public string getExcelDownloadLink()
        {
            return getGeneralInfoString("excelDownloadLink");
        }

        // public string getCallbackData() Implementation in Insert Action

        // ------------------------------------- USER DATA ACTION -------------------------------------
        public string getRegDatabaseName()
        {
            return getDatabaseName();
        }

        public string[] getRegFieldNames()
        {
            return getFieldNames();
        }

        public DatabaseRecord getRegRecord()
        {
            return getRecord(1);
        }

        public string getLastLogin()
        {
            return getGeneralInfoString("lastLogin");
        }

        public SubscriptionInfo getSubscriptionInfo()
        {
            SubscriptionInfo subscriptionInfo = new SubscriptionInfo();

            subscriptionInfo.LoggedInStatus = getGeneralInfoString("loggedInStatus");
            subscriptionInfo.SubscriptionStatus = getGeneralInfoString("subscriptionStatus");

            return subscriptionInfo;
        }

        public SubscriptionDetails getSubscriptionDetails()
        {
            if (isActionValid("userData") == false)
                return null;

            SubscriptionDetails subscriptionDetails = new SubscriptionDetails();

            subscriptionDetails.ServicePlan = getSubscriptionInfoString("servicePlan");
            subscriptionDetails.AccountStatus = getSubscriptionInfoString("accountStatus");

            if (subscriptionDetails.AccountStatus.Equals("Inactive") == false && subscriptionDetails.AccountStatus.Equals("Always Active") == false)
            {
                if (subscriptionDetails.AccountStatus.Equals("Active for a number of days") == true)
                    subscriptionDetails.RemainingDaysActive = getSubscriptionInfoInteger("remainingdaysActive");

                if (subscriptionDetails.AccountStatus.Equals("Active while account balance is positive") == true)
                {
                    subscriptionDetails.CurrentAccountBalance = getSubscriptionInfoString("currentAccountBalance");
                    subscriptionDetails.DailyRate = getSubscriptionInfoString("dailyRate");
                }
            }

            if (subscriptionDetails.AccountStatus.Equals("Inactive") == false)
            {
                subscriptionDetails.UsageRestrictions = getSubscriptionInfoString("usageRestrictions");
                if (subscriptionDetails.UsageRestrictions.Equals("By number of records") == true)
                    subscriptionDetails.RecordStorageRestriction = getSubscriptionInfoString("recordStorageRestriction");
            }

            return subscriptionDetails;
        }

        public int getPaymentCount()
        {
            return getSubscriptionPaymentInfoInteger("paymentCount");
        }

        public int getTotalPaid()
        {
            return getSubscriptionPaymentInfoInteger("totalPaid");
        }

        public SubscriptionPaymentInfo getPaymentInfo(int paymentNumber)
        {
            SubscriptionPaymentInfo subscriptionPaymentInfo = new SubscriptionPaymentInfo();

            int paymentCount = getPaymentCount();
            if (paymentCount > 0 && paymentCount >= paymentNumber && paymentNumber >= 1)
            {
                subscriptionPaymentInfo.PaymentAmount = getSubscriptionPaymentInfoString(paymentNumber, "paymentAmount");
                subscriptionPaymentInfo.ServicePlan = getSubscriptionPaymentInfoString(paymentNumber, "servicePlan");
                subscriptionPaymentInfo.PaymentProcessor = getSubscriptionPaymentInfoString(paymentNumber, "paymentProcessor");
                subscriptionPaymentInfo.TransactionID = getSubscriptionPaymentInfoString(paymentNumber, "transactionID");
                subscriptionPaymentInfo.TransactionDate = getSubscriptionPaymentInfoString(paymentNumber, "transactionDate");
                subscriptionPaymentInfo.Currency = getSubscriptionPaymentInfoString(paymentNumber, "currency");
            }

            return subscriptionPaymentInfo;
        }

        public string getEditRecordPassword()
        {
            return getEditRecordPassword(1);
        }

        public string getRecordID()
        {
            return getRecordID(1);
        }

        // ------------------------------------- RECORD DETAILS ACTION -------------------------------------
        // public string getDatabaseName() Implementation in search action.

        // public string[] getFieldNames() Implementation in search action.

        // public DatabaseRecord getRecord(int recordNumber)  Implementation in search action.

        public string[] getTriggerDatabaseNames()
        {
            return getGeneralInfoArray("trigger");
        }

        public string[] getTriggerFieldNames(string triggerDatabaseName)
        {
            string[] triggerFieldNames = null;
            string[] triggerDatabaseNames = getTriggerDatabaseNames();

            if (triggerDatabaseNames != null)
                for (var i = 0; i < triggerDatabaseNames.Length; i++)
                    if (triggerDatabaseNames[i] == triggerDatabaseName)
                    {
                        triggerFieldNames = getGeneralInfoArray("triggerFieldNames", i);
                        break;
                    }

            return triggerFieldNames;
        }

        public DatabaseRecord getTriggerRecord(string triggerDatabaseName)
        {
            return getTriggerRecordInfo(1, triggerDatabaseName);
        }

        // ------------------------------------- LOGIN ACTION -------------------------------------
        public string getLoginName()
        {
            return getGeneralInfoString("searchName");
        }

        // public string getRegDatabaseName() Implementation in user data action.

        // public string[] getRegFieldNames()  Implementation in user data action.

        // public string[] getRegRecords()  Implementation in user data action.

        // public string[] getLastLogin()  Implementation in user data action.

        // public bool getUserInfo()  Implementation in insert action.

        // public string getEditRecordPassword()  Implementation in user data action.

        // public string getRecordID(int recordNumber) Implementation in user data action.

        // public string[] getTriggerDatabaseNames()  Implementation in record details action.

        // public string[] getTriggerFieldNames(string triggerDatabaseName)  Implementation in record details action.

        // public DatabaseRecord getTriggerRecord(string triggerDatabaseName)  Implementation in record details action.

        // public string getCallbackData()  Implementation in insert action.

        // ------------------------------------- LOGOUT ACTION -------------------------------------
        // public string getCallbackData()  Implementation in insert action.

        // ------------------------------------- GET LOGIN CREDENTIALS ACTION -------------------------------------
        // public string getLoginName()  Implementation in login action.

        // public string getCallbackData()  Implementation in insert action.

        // ------------------------------------- NOTIFICATION REGISTRATION ACTION -------------------------------------
        public string getNotificationDatabaseName()
        {
            return getDatabaseName();
        }

        public string[] getNotificationFieldNames()
        {
            return getFieldNames();
        }

        public DatabaseRecord getNotificationData()
        {
            return getRecord(1);
        }

        // public bool getUserInfo()  Implementation in insert action.

        // public long getDatePosted()   Implementation in insert action.

        // public string getCallbackData()  Implementation in insert action.


        // ------------------------------------- RECORD COUNT ACTION -------------------------------------
        public string getRecordCountDatabaseName()
        {
            return getDatabaseName();
        }

        // public bool getUserInfo()  Implementation in insert action.

        public int getRecordCount()
        {
            return getGeneralInfoInteger("recordCount");
        }

        public RecordCountParameters getOptionalRecordCountParameters()
        {
            RecordCountParameters recordCountParameters = new RecordCountParameters();

            recordCountParameters.FieldName = getGeneralInfoString("fieldName");
            recordCountParameters.CountOperator = getGeneralInfoString("operator");
            recordCountParameters.Comparator = getGeneralInfoString("comparator");

            return recordCountParameters;
        }

        public bool getSinceLastLoginFlag()
        {
            return getGeneralInfoBool("sll");
        }

        // public string getCallbackData()  Implementation in insert action.

        // ------------------------------------- DELETE RECORD ACTION -------------------------------------
        // public string getDatabaseName()  Implementation in search action.

        // public string getSearchName()  Implementation in search action.

        // public string getRecordID()  Implementation in user data action.


        // -------------------------------- ROLLSESSIONPROVIDER RECORD ACTION --------------------------------
        public Hashtable getFileFieldInfo()
        {
            Hashtable fileFiledData = null;

            string[] fileDataFieldNames = getGeneralInfoArray("fileDataFieldNames");
            if (fileDataFieldNames != null)
            {
                string[] fileDataTableNames = getGeneralInfoArray("fileDataTableNames");
                string[] fileDataSizeLimits = getGeneralInfoArray("fileDataSizeLimits");

                fileFiledData = new Hashtable();
                for (int i = 0; i < fileDataFieldNames.Length; i++)
                    fileFiledData.Add(fileDataTableNames[i] + fileDataFieldNames[i], Convert.ToInt64(fileDataSizeLimits[i]));
            }

            return fileFiledData;
        }

        /******************************************* END PUBLIC API *******************************************/

    }

    public class defines
    {
        public const int SUCCESS = 0;
        public const int GENERAL_ERROR = -1;
        public const int SUBSCRIPTION_RECORD_LIMIT_EXCEPTION = 1;
        public const int INVALID_CAPTCHA_EXCEPTION = 2;
        public const int INVALID_DATA_EXCEPTION = 3;
        public const int NOT_UNIQUE_DATA_EXCEPTION = 4;
        public const int ACCESS_DENIED_EXCEPTION = 5;
        public const int FILE_SIZE_LIMIT_EXCEPTION = 6;
        public const int DB_FULL_EXCEPTION = 7;
        public const int BAD_OR_MISSING_ID_EXCEPTION = 8;
        public const int NO_RECORDS_FOUND_EXCEPTION = 9;
        public const int RECORD_NOT_FOUND_EXCEPTION = 10;
        public const int SESSION_TIMEOUT_EXCEPTION = 11;
        public const int UNAUTHORIZED_ACCESS_EXCEPTION = 12;
        public const int LOGIN_CREDENTIALS_NOT_FOUND = 13;
        public const int LOGIN_NOT_FOUND_EXCEPTION = 14;
        public const int INVALID_EMAIL_ADDRESS_EXCEPTION = 15;
        public const int MULTIPART_LIMIT_EXCEPTION = 16;
        public const int IP_ADDRESS_INSERT_RESTRICTION = 17;
        public const int INVALID_REQUEST = 18;
        public const int ANONYMOUS_USER_EXCEPTION = 19;
        public const int INVALID_UPDATE_CREDENTIALS = 20;
    }

    public class RecordCountParameters
    {
        private string fieldName;
        public string FieldName
        {
            get { return fieldName; }
            set { fieldName = value; }
        }
        private string countOperator;
        public string CountOperator
        {
            get { return countOperator; }
            set { countOperator = value; }
        }
        private string comparator;
        public string Comparator
        {
            get { return comparator; }
            set { comparator = value; }
        }
    }

    public class SubscriptionPaymentInfo
    {
        private string paymentAmount;
        public string PaymentAmount
        {
            get { return paymentAmount; }
            set { paymentAmount = value; }
        }
        private string servicePlan;
        public string ServicePlan
        {
            get { return servicePlan; }
            set { servicePlan = value; }
        }
        private string paymentProcessor;
        public string PaymentProcessor
        {
            get { return paymentProcessor; }
            set { paymentProcessor = value; }
        }
        private string transactionID;
        public string TransactionID
        {
            get { return transactionID; }
            set { transactionID = value; }
        }
        private string transactionDate;
        public string TransactionDate
        {
            get { return transactionDate; }
            set { transactionDate = value; }
        }
        private string currency;
        public string Currency
        {
            get { return currency; }
            set { currency = value; }
        }
    }

    public class SubscriptionDetails
    {
        private string servicePlan;
        public string ServicePlan
        {
            get { return servicePlan; }
            set { servicePlan = value; }
        }
        private string accountStatus;
        public string AccountStatus
        {
            get { return accountStatus; }
            set { accountStatus = value; }
        }
        private int remainingDaysActive;
        public int RemainingDaysActive
        {
            get { return remainingDaysActive; }
            set { remainingDaysActive = value; }
        }
        private string currentAccountBalance;
        public string CurrentAccountBalance
        {
            get { return currentAccountBalance; }
            set { currentAccountBalance = value; }
        }
        private string dailyRate;
        public string DailyRate
        {
            get { return dailyRate; }
            set { dailyRate = value; }
        }
        private string usageRestrictions;
        public string UsageRestrictions
        {
            get { return usageRestrictions; }
            set { usageRestrictions = value; }
        }
        private string recordStorageRestriction;
        public string RecordStorageRestriction
        {
            get { return recordStorageRestriction; }
            set { recordStorageRestriction = value; }
        }
    }

    public class SubscriptionInfo
    {
        private string loggedInStatus;
        public string LoggedInStatus
        {
            get { return loggedInStatus; }
            set { loggedInStatus = value; }
        }
        private string subscriptionStatus;
        public string SubscriptionStatus
        {
            get { return subscriptionStatus; }
            set { subscriptionStatus = value; }
        }
    }

    public class DatabaseRecord
    {
        private FieldInfo[] fieldInfo;

        public DatabaseRecord(int fieldCount)
        {
            fieldInfo = new FieldInfo[fieldCount];
        }

        public void add(FieldInfo fieldInfo)
        {
            for (int i = 0; i < this.fieldInfo.Length; i++)
                if (this.fieldInfo[i] == null)
                {
                    this.fieldInfo[i] = fieldInfo;
                    break;
                }
        }

        public FieldInfo getFieldInfo(string fieldName)
        {
            for (int i = 0; i < this.fieldInfo.Length; i++)
                if (this.fieldInfo[i] != null)
                {
                    if (this.fieldInfo[i].FieldName.Equals(fieldName) == true)
                        return this.fieldInfo[i];
                }

            return null;
        }
    }

    public class FieldInfo
    {
        private string[] fieldData;
        public string this[int i]
        {
            get
            {
                return fieldData[i];
            }
            set
            {
                fieldData[i] = value;
            }
        }
        internal void allocateFieldData(int size)
        {
            fieldData = new string[size];
        }

        private string dataType;
        public string DataType
        {
            get { return dataType; }
            set { dataType = value; }
        }
        private int dataCount;
        public int DataCount
        {
            get { return dataCount; }
            set { dataCount = value; }
        }
        private string sortLink;
        public string SortLink
        {
            get { return sortLink; }
            set { sortLink = value; }
        }
        private string fieldName;
        public string FieldName
        {
            get { return fieldName; }
            set { fieldName = value; }
        }
        private string databaseName;
        public string DatabaseName
        {
            get { return databaseName; }
            set { databaseName = value; }
        }
        private string fileLinkURL;
        public string FileLinkURL
        {
            get { return fileLinkURL; }
            set { fileLinkURL = value; }
        }
        private bool isPicture;
        public bool IsPicture
        {
            get { return isPicture; }
            set { isPicture = value; }
        }
        private string thumbnailLinkURL;
        public string ThumbnailLinkURL
        {
            get { return thumbnailLinkURL; }
            set { thumbnailLinkURL = value; }
        }
    }

    public class RecordCountDetails
    {
        private int recordCount;
        public int RecordCount
        {
            get { return recordCount; }
            set { recordCount = value; }
        }
        private int pageCount;
        public int PageCount
        {
            get { return pageCount; }
            set { pageCount = value; }
        }
        private int pageNumber;
        public int PageNumber
        {
            get { return pageNumber; }
            set { pageNumber = value; }
        }
        private int totalRecordsFound;
        public int TotalRecordsFound
        {
            get { return totalRecordsFound; }
            set { totalRecordsFound = value; }
        }
    }

    public class ResultDetails
    {
        private int resultCode;
        public int ResultCode
        {
            get { return resultCode; }
            set { resultCode = value; }
        }
        private string resultText;
        public string ResultText
        {
            get { return resultText; }
            set { resultText = value; }
        }
        private string resultMessage;
        public string ResultMessage
        {
            get { return resultMessage; }
            set { resultMessage = value; }
        }
        private string processingTimeStamp;
        public string ProcessingTimeStamp
        {
            get { return processingTimeStamp; }
            set { processingTimeStamp = value; }
        }
    }

    public class HotRiotJSON : JObject
    {
        public HotRiotJSON(JObject jObject)
            : base(jObject)
        {
        }
    }
    public class HRInsertResponse : HRResponse
    {
        public HRInsertResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRSearchResponse : HRResponse
    {
        public HRSearchResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRLoginResponse : HRResponse
    {
        public HRLoginResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRLoginLookupResponse : HRResponse
    {
        public HRLoginLookupResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRNotificationResponse : HRResponse
    {
        public HRNotificationResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRLogoutResponse : HRResponse
    {
        public HRLogoutResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRRecordCountResponse : HRResponse
    {
        public HRRecordCountResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRUserDataResponse : HRResponse
    {
        public HRUserDataResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRGetTriggerResponse : HRResponse
    {
        public HRGetTriggerResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRDeleteResponse : HRResponse
    {
        public HRDeleteResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRPushServiceResponse : HRResponse
    {
        public HRPushServiceResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }
    public class HRRollSessionResponse : HRResponse
    {
        public HRRollSessionResponse(HotRiotJSON hotRiotJSON)
            : base(hotRiotJSON)
        {
        }
    }

    public class HotRiotException : Exception
    {
        public HotRiotException()
        {
        }

        public HotRiotException(string message)
            : base(message)
        {
        }

        public HotRiotException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class HTTPProgresss
    {
        private long totalBytesProcessed;
        public long TotalBytesProcessed
        {
            get { return totalBytesProcessed; }
            set { totalBytesProcessed = value; }
        }
        private long totalBytesToProcess;
        public long TotalBytesToProcess
        {
            get { return totalBytesToProcess; }
            set { totalBytesToProcess = value; }
        }
        private long elapsTimeInMillis;
        public long ElapsTimeInMillis
        {
            get { return elapsTimeInMillis; }
            set { elapsTimeInMillis = value; }
        }
        private long bytesProcessed;
        public long BytesProcessed
        {
            get { return bytesProcessed; }
            set { bytesProcessed = value; }
        }
        private long startTime;
        public long StartTime
        {
            get { return startTime; }
            set { startTime = value; }
        }
    }

    public class FileMetadata
    {
        private long contentLength;
        public long ContentLength
        {
            get { return contentLength; }
            set { contentLength = value; }
        }
        private string contentType;
        public string ContentType
        {
            get { return contentType; }
            set { contentType = value; }
        }
        private bool isFromCache;
        public bool IsFromCache
        {
            get { return isFromCache; }
            set { isFromCache = value; }
        }
        private string date;
        public string Date
        {
            get { return date; }
            set { date = value; }
        }
        private string lastModified;
        public string LastModified
        {
            get { return lastModified; }
            set { lastModified = value; }
        }
    }

    public class PutDocumentCredentials
    {
        public string aKey;
        public string sKey;
        public string key;
        public string bucket;
        public int thumbnailSize;
        public string sessionToken;
        public long creationTime;
    }

    public class helpers
    {
        public static string GetUniqueKey(int maxSize)
        {
            byte[] data = new byte[1];
            char[] chars = new char[62];
            chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

            RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            data = new byte[maxSize];
            crypto.GetNonZeroBytes(data);
            StringBuilder result = new StringBuilder(maxSize);

            foreach (byte b in data)
                result.Append(chars[b % (chars.Length)]);

            return result.ToString();
        }
    }

    public class PostRequestParam
    {
        internal string url;
        internal NameValueCollection nvc;
        internal NameValueCollection files;
        internal string databaseName;

        public PostRequestParam(string url, NameValueCollection nvc, NameValueCollection files, string databaseName)
        {
            this.url = url;
            this.nvc = nvc;
            this.files = files;
            this.databaseName = databaseName;
        }

        public PostRequestParam(string url, NameValueCollection nvc)
        {
            this.url = url;
            this.nvc = nvc;
        }
    }

    public class PutObjectRequestLocal
    {
        public string BucketName { get; set; }
        public string FilePath { get; set; }
        public string Key { get; set; }
    }

    // "Create Pre Signed URL Using C# for Uploading Large Files In Amazon S3."
    // This class is an adaptation from code posted on the following blog, 
    // http://gauravmantri.com/2014/01/06/create-pre-signed-url-using-c-for-uploading-large-files-in-amazon-s3/
    // Many thanks to Gaurav Mantri for making this available. http://gauravmantri.com
    class S3
    {
        private static string[] subResourcesToConsider = new string[] { "acl", "lifecycle", "location", "logging", "notification", "partNumber", "policy", "requestPayment", "torrent", "uploadId", "uploads", "versionId", "versioning", "versions", "website", };
        private static string[] overrideResponseHeadersToConsider = new string[] { "response-content-type", "response-content-language", "response-expires", "response-cache-control", "response-content-disposition", "response-content-encoding" };
        private static int BUFFER_LENGTH = 4096;
        private static int FIVE_MB = 5 * 1024 * 1024;


        public static void putFile(string accessKey, string secretKey, Stream memStream, string fileName, string bucketName, string securityToken)
        {
            var requestUri = new Uri("https://s3-external-1.amazonaws.com/" + bucketName + "/" + fileName);
            var expiryDate = DateTime.UtcNow.AddHours(1);

            var uploadId = InitiateMultipartUpload(accessKey, secretKey, requestUri, DateTime.UtcNow.AddMinutes(10), "", securityToken);
            var partNumberETags = UploadParts(accessKey, secretKey, requestUri, uploadId, memStream, expiryDate, securityToken);
            FinishMultipartUpload(accessKey, secretKey, requestUri, uploadId, partNumberETags, expiryDate, securityToken);
        }

        private static string GetStringToSign(Uri requestUri, string httpVerb, string contentMD5, string contentType, DateTime date, NameValueCollection requestHeaders)
        {
            var canonicalizedResourceString = GetCanonicalizedResourceString(requestUri);
            var canonicalizedAmzHeadersString = GetCanonicalizedAmzHeadersString(requestHeaders);
            var dateInStringFormat = date.ToString("R");
            if (requestHeaders != null && requestHeaders.AllKeys.Contains("x-amz-date"))
                dateInStringFormat = string.Empty;

            var stringToSign = string.Format("{0}\n{1}\n{2}\n{3}\n{4}{5}", httpVerb, contentMD5, contentType, dateInStringFormat, canonicalizedAmzHeadersString, canonicalizedResourceString);
            return stringToSign;
        }

        private static string GetStringToSign(Uri requestUri, string httpVerb, string contentMD5, string contentType, long secondsSince1stJan1970, NameValueCollection requestHeaders)
        {
            var canonicalizedResourceString = GetCanonicalizedResourceString(requestUri);
            var canonicalizedAmzHeadersString = GetCanonicalizedAmzHeadersString(requestHeaders);
            var stringToSign = string.Format("{0}\n{1}\n{2}\n{3}\n{4}{5}", httpVerb, contentMD5, contentType, secondsSince1stJan1970, canonicalizedAmzHeadersString, canonicalizedResourceString);

            return stringToSign;
        }

        private static string GetCanonicalizedResourceString(Uri requestUri)
        {
            var host = requestUri.DnsSafeHost;
            var hostElementsArray = host.Split('.');
            var bucketName = "";
            if (hostElementsArray.Length > 3)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hostElementsArray.Length - 3; i++)
                {
                    sb.AppendFormat("{0}.", hostElementsArray[i]);
                }
                bucketName = sb.ToString();
                if (bucketName.Length > 0)
                {
                    if (bucketName.EndsWith("."))
                    {
                        bucketName = bucketName.Substring(0, bucketName.Length - 1);
                    }
                    bucketName = string.Format("/{0}", bucketName);
                }
            }

            var subResourcesList = subResourcesToConsider.ToList();
            var overrideResponseHeadersList = overrideResponseHeadersToConsider.ToList();
            StringBuilder canonicalizedResourceStringBuilder = new StringBuilder();
            canonicalizedResourceStringBuilder.Append(bucketName);
            canonicalizedResourceStringBuilder.Append(requestUri.AbsolutePath);
            NameValueCollection queryVariables = HttpUtility.ParseQueryString(requestUri.Query);
            SortedDictionary<string, string> queryVariablesToConsider = new SortedDictionary<string, string>();
            SortedDictionary<string, string> overrideResponseHeaders = new SortedDictionary<string, string>();
            if (queryVariables != null && queryVariables.Count > 0)
            {
                var numQueryItems = queryVariables.Count;
                for (int i = 0; i < numQueryItems; i++)
                {
                    var key = queryVariables.GetKey(i);
                    var value = queryVariables[key];
                    if (subResourcesList.Contains(key))
                    {
                        if (queryVariablesToConsider.ContainsKey(key))
                        {
                            var val = queryVariablesToConsider[key];
                            queryVariablesToConsider[key] = string.Format("{0},{1}", value, val);
                        }
                        else
                        {
                            queryVariablesToConsider.Add(key, value);
                        }
                    }
                    if (overrideResponseHeadersList.Contains(key))
                    {
                        overrideResponseHeaders.Add(key, HttpUtility.UrlDecode(value));
                    }
                }
            }
            if (queryVariablesToConsider.Count > 0 || overrideResponseHeaders.Count > 0)
            {
                StringBuilder queryStringInCanonicalizedResourceString = new StringBuilder();
                queryStringInCanonicalizedResourceString.Append("?");
                for (int i = 0; i < queryVariablesToConsider.Count; i++)
                {
                    var key = queryVariablesToConsider.Keys.ElementAt(i);
                    var value = queryVariablesToConsider.Values.ElementAt(i);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        queryStringInCanonicalizedResourceString.AppendFormat("{0}={1}&", key, value);
                    }
                    else
                    {
                        queryStringInCanonicalizedResourceString.AppendFormat("{0}&", key);
                    }
                }
                for (int i = 0; i < overrideResponseHeaders.Count; i++)
                {
                    var key = overrideResponseHeaders.Keys.ElementAt(i);
                    var value = overrideResponseHeaders.Values.ElementAt(i);
                    queryStringInCanonicalizedResourceString.AppendFormat("{0}={1}&", key, value);
                }
                var str = queryStringInCanonicalizedResourceString.ToString();
                if (str.EndsWith("&"))
                {
                    str = str.Substring(0, str.Length - 1);
                }
                canonicalizedResourceStringBuilder.Append(str);
            }
            return canonicalizedResourceStringBuilder.ToString();
        }

        private static string GetCanonicalizedAmzHeadersString(NameValueCollection requestHeaders)
        {
            var canonicalizedAmzHeadersString = string.Empty;
            if (requestHeaders != null && requestHeaders.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                SortedDictionary<string, string> sortedRequestHeaders = new SortedDictionary<string, string>();
                var requestHeadersCount = requestHeaders.Count;
                for (int i = 0; i < requestHeadersCount; i++)
                {
                    var key = requestHeaders.Keys.Get(i);
                    var value = requestHeaders[key].Trim();
                    key = key.ToLowerInvariant();
                    if (key.StartsWith("x-amz-", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (sortedRequestHeaders.ContainsKey(key))
                        {
                            var val = sortedRequestHeaders[key];
                            sortedRequestHeaders[key] = string.Format("{0},{1}", val, value);
                        }
                        else
                        {
                            sortedRequestHeaders.Add(key, value);
                        }
                    }
                }
                if (sortedRequestHeaders.Count > 0)
                {
                    foreach (var item in sortedRequestHeaders)
                    {
                        sb.AppendFormat("{0}:{1}\n", item.Key, item.Value);
                    }
                    canonicalizedAmzHeadersString = sb.ToString();
                }
            }
            return canonicalizedAmzHeadersString;
        }

        private static string CreateSignature(string secretKey, string stringToSign)
        {
            byte[] dataToSign = Encoding.UTF8.GetBytes(stringToSign);
            using (HMACSHA1 hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey)))
            {
                return Convert.ToBase64String(hmacsha1.ComputeHash(dataToSign));
            }
        }

        private static string InitiateMultipartUpload(string accessKey, string secretKey, Uri requestUri, DateTime requestDate, string contentType, string securityToken)
        {
            var uploadId = string.Empty;
            var uploadIdRequestUrlRequestHeaders = new NameValueCollection();
            var uploadIdRequestUrl = new Uri(string.Format("{0}?uploads=", requestUri.AbsoluteUri));

            uploadIdRequestUrlRequestHeaders.Add("x-amz-security-token", securityToken);
            var stringToSign = GetStringToSign(uploadIdRequestUrl, "POST", string.Empty, contentType, requestDate, uploadIdRequestUrlRequestHeaders);
            var signatureForUploadId = CreateSignature(secretKey, stringToSign);
            uploadIdRequestUrlRequestHeaders.Add("Authorization", string.Format("AWS {0}:{1}", accessKey, signatureForUploadId));

            var request = (HttpWebRequest)WebRequest.Create(uploadIdRequestUrl);
            request.Method = "POST";
            request.ContentLength = 0;
            request.Date = requestDate;
            request.ContentType = contentType;
            request.Headers.Add(uploadIdRequestUrlRequestHeaders);
            using (var resp = (HttpWebResponse)request.GetResponse())
            {
                using (var s = new StreamReader(resp.GetResponseStream()))
                {
                    var response = s.ReadToEnd();
                    XElement xe = XElement.Parse(response);
                    uploadId = xe.Element(XName.Get("UploadId", "http://s3.amazonaws.com/doc/2006-03-01/")).Value;
                }
            }

            return uploadId;
        }

        private static Dictionary<int, string> UploadParts(string accessKey, string secretKey, Uri requestUri, string uploadId, Stream memStream, DateTime expiryDate, string securityToken)
        {
            Dictionary<int, string> partNumberETags = new Dictionary<int, string>();
            TimeSpan ts = new TimeSpan(expiryDate.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks);

            var expiry = Convert.ToInt64(ts.TotalSeconds);
            long initialBufferSize = BUFFER_LENGTH;
            int startPosition = 0;
            int partNumber = 1;

            int bytesToBeUploaded = (int)memStream.Length;
            byte[] fileContents = new byte[initialBufferSize];

            NameValueCollection uploadIdRequestUrlRequestHeaders = new NameValueCollection();
            uploadIdRequestUrlRequestHeaders.Add("x-amz-security-token", securityToken);
            do
            {
                int bytesToUpload = Math.Min(FIVE_MB, bytesToBeUploaded);
                var partUploadUrl = new Uri(string.Format("{0}?uploadId={1}&partNumber={2}", requestUri.AbsoluteUri, HttpUtility.UrlEncode(uploadId), partNumber));
                var partUploadSignature = CreateSignature(secretKey, GetStringToSign(partUploadUrl, "PUT", string.Empty, string.Empty, expiry, uploadIdRequestUrlRequestHeaders));
                var partUploadPreSignedUrl = new Uri(string.Format("{0}?uploadId={1}&partNumber={2}&AWSAccessKeyId={3}&Signature={4}&Expires={5}", requestUri.AbsoluteUri,
                                                     HttpUtility.UrlEncode(uploadId), partNumber, accessKey, HttpUtility.UrlEncode(partUploadSignature), expiry));
                var request = (HttpWebRequest)WebRequest.Create(partUploadPreSignedUrl);
                request.Method = "PUT";
                request.Timeout = 1000 * 600;
                request.ContentLength = bytesToUpload;
                request.Headers.Add(uploadIdRequestUrlRequestHeaders);

                using (var stream = request.GetRequestStream())
                {
                    memStream.Position = startPosition;
                    int offset = 0;
                    if (initialBufferSize > bytesToBeUploaded)
                        initialBufferSize = bytesToBeUploaded;

                    while (offset < bytesToUpload)
                    {
                        int bytesRead = memStream.Read(fileContents, 0, (int)initialBufferSize);
                        stream.Write(fileContents, 0, bytesRead);
                        offset += bytesRead;
                    }
                }

                using (var resp = (HttpWebResponse)request.GetResponse())
                {
                    using (var s = new StreamReader(resp.GetResponseStream()))
                    {
                        partNumberETags.Add(partNumber, resp.Headers["ETag"]);
                    }
                }

                bytesToBeUploaded = bytesToBeUploaded - bytesToUpload;
                startPosition = bytesToUpload;
                partNumber = partNumber + 1;
            }while (bytesToBeUploaded > 0);

            return partNumberETags;
        }

        private static void FinishMultipartUpload(string accessKey, string secretKey, Uri requestUri, string uploadId, Dictionary<int, string> partNumberETags, DateTime expiryDate, string securityToken)
        {
            TimeSpan ts = new TimeSpan(expiryDate.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks);
            var expiry = Convert.ToInt64(ts.TotalSeconds);
            var finishOrCancelMultipartUploadUri = new Uri(string.Format("{0}?uploadId={1}", requestUri.AbsoluteUri, uploadId));
            NameValueCollection uploadIdRequestUrlRequestHeaders = new NameValueCollection();
            uploadIdRequestUrlRequestHeaders.Add("x-amz-security-token", securityToken);
            var signatureForFinishMultipartUpload = CreateSignature(secretKey, GetStringToSign(finishOrCancelMultipartUploadUri, "POST", string.Empty, "text/plain", expiry, uploadIdRequestUrlRequestHeaders));
            var finishMultipartUploadUrl = new Uri(string.Format("{0}?uploadId={1}&AWSAccessKeyId={2}&Signature={3}&Expires={4}", requestUri.AbsoluteUri, HttpUtility.UrlEncode(uploadId), accessKey, HttpUtility.UrlEncode(signatureForFinishMultipartUpload), expiry));
            StringBuilder payload = new StringBuilder();
            payload.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?><CompleteMultipartUpload>");
            foreach (var item in partNumberETags)
            {
                payload.AppendFormat("<Part><PartNumber>{0}</PartNumber><ETag>{1}</ETag></Part>", item.Key, item.Value);
            }
            payload.Append("</CompleteMultipartUpload>");
            var requestPayload = Encoding.UTF8.GetBytes(payload.ToString());
            var request = (HttpWebRequest)WebRequest.Create(finishMultipartUploadUrl);
            request.Method = "POST";
            request.ContentType = "text/plain";
            request.ContentLength = requestPayload.Length;
            request.Headers.Add(uploadIdRequestUrlRequestHeaders);
            using (var stream = request.GetRequestStream())
            {
                stream.Write(requestPayload, 0, requestPayload.Length);
            }
            using (var resp = (HttpWebResponse)request.GetResponse())
            {
            }
        }
    }
}
