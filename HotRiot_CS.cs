// HMAC Authentication Info: http://jokecamp.wordpress.com/2012/10/21/examples-of-creating-base64-hashes-using-hmac-sha256-in-different-languages/
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

// Disable variable not used warning for exceptions.
#pragma warning disable 0168  

namespace HotRiot_CS
{
    public sealed class HotRiot : defines
    {
        private static HotRiot HRInstance = new HotRiot();
        private static string PROTOCOL = "https://";

        private string fullyQualifiedHRDAURL;
        private string fullyQualifiedHRURL;
        private string jSessionID;
        private string hmKey;

        private HotRiot(){}

        internal static HotRiot getHotRiotInstance
        {
            get
            {
                return HRInstance;
            }
        }

        internal async Task<HotRiotJSON> postLink(string link)
        {
            WebResponse webResponse = null;
            Stream requestStream = null;
            HotRiotJSON jsonResponse = null;
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
               
            catch(WebException ex )
            {
                throw new HotRiotException("WebException", ex);
            }
            catch(ArgumentNullException ex )
            {
                throw new HotRiotException("ArgumentNullException", ex);
            }
            catch(OutOfMemoryException ex)
            {
                throw new HotRiotException("OutOfMemoryException", ex);
            }
            catch(IOException ex)
            {
                throw new HotRiotException("IOException", ex);
            }
            catch(ArgumentOutOfRangeException ex)
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

        internal async Task<HotRiotJSON> postRequest(string url, NameValueCollection nvc, NameValueCollection files)
        {
            WebResponse webResponse = null;
            FileStream fileStream = null;
            Stream requestStream = null;
            HotRiotJSON jsonResponse = null;
            StreamReader reader = null;
            Stream stream = null;

            try
            {
                string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url + jSessionID);
                httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                httpWebRequest.Method = "POST";
                httpWebRequest.KeepAlive = true;
                httpWebRequest.Credentials =
                System.Net.CredentialCache.DefaultCredentials;

                byte[] boundarybytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "\r\n");
                string formdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";

                requestStream = await httpWebRequest.GetRequestStreamAsync();
                foreach (string key in nvc.Keys)
                {
                    string formitem = string.Format(formdataTemplate, key, nvc[key]);
                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                    requestStream.Write(formitembytes, 0, formitembytes.Length);
                }

                requestStream.Write(boundarybytes, 0, boundarybytes.Length);

                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";

                if (files != null)
                {
                    byte[] buffer = new byte[4096];

                    foreach(string key in files.Keys)
                    {
                        string header = string.Format(headerTemplate, key, files[key]);
                        byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                        requestStream.Write(headerbytes, 0, headerbytes.Length);

                        fileStream = new FileStream(files[key], FileMode.Open, FileAccess.Read);
                        int bytesRead = 0;
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                            requestStream.Write(buffer, 0, bytesRead);
                        requestStream.Write(boundarybytes, 0, boundarybytes.Length);

                        fileStream.Dispose();
                        fileStream.Close();
                        fileStream = null;
                    }
                }
                requestStream.Dispose();
                requestStream.Close();
                requestStream = null;

                webResponse = await httpWebRequest.GetResponseAsync();
                stream = webResponse.GetResponseStream();
                reader = new StreamReader(stream);
                jsonResponse = processResponse(reader.ReadToEnd());
            }
            catch(WebException ex)
            {
                throw new HotRiotException("WebException", ex);
            }
            catch(ArgumentNullException ex)
            {
                throw new HotRiotException("ArgumentNullException", ex);
            }
            catch(OutOfMemoryException ex)
            {
                throw new HotRiotException("OutOfMemoryException", ex);
            }
            catch(ArgumentException ex)
            {
                throw new HotRiotException("ArgumentException", ex);
            }
            catch(FileNotFoundException ex)
            {
                throw new HotRiotException( "FileNotFoundException", ex );
            }
            catch(DirectoryNotFoundException ex)
            {
                throw new HotRiotException("DirectoryNotFoundException", ex);
            }
            catch(IOException ex)
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
                if (fileStream != null )
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
            recordData.Set("hsp-formname", databaseName);
            return new HRInsertResponse(await postRequest(fullyQualifiedHRURL, recordData, files));
        }

        public async Task<HRInsertResponse> submitUpdateRecord(string databaseName, string recordID, string updatePassword, NameValueCollection recordData, NameValueCollection files)
        {
            recordData.Set("hsp-formname", databaseName);
            recordData.Set("hsp-json", updatePassword);
            recordData.Set("hsp-recordID", recordID);
            return new HRInsertResponse(await postRequest(fullyQualifiedHRURL, recordData, files));
        }

        public async Task<HRSearchResponse> submitSearch(string searchName, NameValueCollection searchCriterion)
        {
            searchCriterion.Set("hsp-formname", searchName);
            return new HRSearchResponse(await postRequest(fullyQualifiedHRURL, searchCriterion, null));
        }

        public async Task<HRLoginResponse> submitLogin(string loginName, NameValueCollection loginCredentials)
        {
            loginCredentials.Set("hsp-formname", loginName);
            return new HRLoginResponse(await postRequest(fullyQualifiedHRURL, loginCredentials, null));
        }

        public async Task<HRNotificationResponse> submitNotification(string databaseName, NameValueCollection notificationData)
        {
            notificationData.Set("hsp-formname", databaseName);
            notificationData.Set("hsp-rtninsert", "1");
            return new HRNotificationResponse(await postRequest(fullyQualifiedHRURL, notificationData, null));
        }

        public async Task<HRLoginLookupResponse> submitLostLoginLookup(string loginName, NameValueCollection loginLookupData)
        {
            loginLookupData.Set("hsp-formname", loginName);
            return new HRLoginLookupResponse(await postRequest(fullyQualifiedHRURL, loginLookupData, null));
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
            return await postRequest(fullyQualifiedHRURL, recordCountObject, null);
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
            if(action != null && action.Equals(validAction) == true)
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
            catch (ArgumentNullException doNothing){ }

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

        private bool isValidRecordNumber( int recordNumber)
        {
            if( recordNumber > 0 )
                if (recordNumber <= getGeneralInfoInteger("recordCount") )
                    return true;

            return false;
        }

        private string getFieldDataString( int recordNumber, string dbFieldName)
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
            if(data != null)
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

                if(recordInfo.DataCount != 0)
                {
                    JArray valueString = (JArray)jFieldInfo["value"];
                    recordInfo.allocateFieldData(valueString.Count);
                    for (int i = 0; i < valueString.Count; i++)
                        recordInfo[i] = (String)valueString[i];

                    recordInfo.SortLink = (string)jFieldInfo["sortLink"];
                    if (recordInfo.DataType == "File")
                    {
                        recordInfo.FileLinkURL = (string)jFieldInfo["fileLinkURL"];
                        if((recordInfo.IsPicture = isImage(recordInfo[0])) == true)
                            recordInfo.ThumbnailLinkURL = (string)jFieldInfo["thumbnailLinkURL"];
                    }
                    else
                        recordInfo.IsPicture = false;
                }
            }

            return recordInfo;
        }

        private bool isImage( string filename )
        {
            string[] parts = filename.Split('.');
            if( parts.Length > 1 )
            {
                string extension = parts[parts.Length-1].ToLower();
                if( extension.Equals("jpg") == true || extension.Equals("jpeg") == true )
                    return true;
            }

            return false;
        }

        private string getJoinRecordSystemFieldData(int recordNumber, string systemFieldName, string databaseName)
        {
            string fieldData = null;

            string dbFieldName = databaseName + "::" + systemFieldName;

            if( isValidRecordNumber(recordNumber) == true )
                fieldData = getFieldDataString(recordNumber, dbFieldName);

            return fieldData;
        }

        private DatabaseRecord getTriggerRecordInfo(int recordNumber, string triggerDatabaseName)
        {
            DatabaseRecord databaseRecord = null;

            if( isValidRecordNumber(recordNumber) == true )
            {
                var triggerDatabaseFieldNames = getTriggerFieldNames(triggerDatabaseName);
                if( triggerDatabaseFieldNames != null && triggerDatabaseFieldNames.Length > 0)
                {
                    databaseRecord = new DatabaseRecord( triggerDatabaseFieldNames.Length );

                    for(int i=0; i<triggerDatabaseFieldNames.Length; i++)
                        databaseRecord.add( getDatabaseFieldInfo(recordNumber, triggerDatabaseFieldNames[i], triggerDatabaseName) );
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

            if( loggedInUserInfoLink != null )
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

            if( joinDatabaseNames != null )
                for( var i=0; i<joinDatabaseNames.Length; i++ )
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

            if( isValidRecordNumber(recordNumber) == true )
            {
                string databaseName = getDatabaseName();
                string[] databaseFieldNames = getFieldNames();

                if (databaseFieldNames != null && databaseName != null)
                {
                    if (databaseFieldNames.Length > 0 )
                        databaseRecord = new DatabaseRecord( databaseFieldNames.Length );

                    for(var i=0; i<databaseFieldNames.Length; i++)
                        databaseRecord.add( getDatabaseFieldInfo(recordNumber, databaseFieldNames[i], databaseName) );
                }
            }

            return databaseRecord;
        }

        public DatabaseRecord getJoinRecord( int recordNumber, string joinDatabaseName)
        {
            DatabaseRecord databaseRecord = null;

            if( isValidRecordNumber(recordNumber) == true )
            {
                string[] joinDatabaseFieldNames = getJoinFieldNames(joinDatabaseName);
                if( joinDatabaseFieldNames.Length > 0 )
                {
                    databaseRecord = new DatabaseRecord( joinDatabaseFieldNames.Length );

                    for (var i = 0; i < joinDatabaseFieldNames.Length; i++)
                        databaseRecord.add(getDatabaseFieldInfo(recordNumber, joinDatabaseFieldNames[i], joinDatabaseName));
                }
            }

            return databaseRecord;
        }

        public async Task<HRGetTriggerResponse> getTriggerRecords(int recordNumber)
        {
            HRGetTriggerResponse jsonRecordDetailsResponse = null;

            if( isValidRecordNumber(recordNumber) == true )
            {
                string recordLink = getRecordDataString(recordNumber, "recordLink");
                if(recordLink != null)
                    jsonRecordDetailsResponse = new HRGetTriggerResponse(await HotRiot.getHotRiotInstance.postLink(recordLink));
            }

            return jsonRecordDetailsResponse;
        }

        public async Task<HRSearchResponse> sortSearchResults(string fieldName)
        {
            return await sortSearchResultsEx(null, fieldName );
        }

        public async Task<HRSearchResponse> sortSearchResultsEx(string databaseName, string fieldName)
        {
            FieldInfo recordInfo;

            if( databaseName == null )
            {
                databaseName = getDatabaseName();
                recordInfo = getDatabaseFieldInfo(1, fieldName, databaseName);

                // If I could not find the fieldname in the primary database, chack to see if it exists in any joined databases.
                if(recordInfo == null)
                {
                    string[] joinDatabaseNames = getJoinDatabaseNames();
                    if( joinDatabaseNames != null )
                        for(var i=0; i<joinDatabaseNames.Length; i++)
                        {
                            recordInfo = getDatabaseFieldInfo(1, fieldName, joinDatabaseNames[i]);
                            if(recordInfo != null)
                                break;
                        }
                }

                // If I could not find the fieldname in the primary database or any of the joined databases, chack to see if it exists in any trigger databases.
                if (recordInfo == null)
                {
                    string[] triggerDatabaseNames = getTriggerDatabaseNames();
                    if(triggerDatabaseNames != null)
                        for(var x=0; x<triggerDatabaseNames.Length; x++)
                        {
                            recordInfo = getDatabaseFieldInfo(1, fieldName, triggerDatabaseNames[x]);
                            if(recordInfo != null)
                                break;
                        }
                    }

                // If a record was found with the fieldName, then post the sort link.
                if(recordInfo != null)
                    return new HRSearchResponse(await HotRiot.getHotRiotInstance.postLink(recordInfo.SortLink));
            }
            else
            {
                recordInfo = getDatabaseFieldInfo(1, fieldName, databaseName);
                if(recordInfo != null)
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

            if( pageNumber != 0 && pageCount != 0 && pageNumber < pageCount )
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
            string deleteRecordCommand = getDeleteRecordCommand(recordNumber);
            if (deleteRecordCommand != null)
                return await deleteRecordDirect(deleteRecordCommand, repost);

            return null;
        }

        public async Task<HotRiotJSON> deleteJoinRecord(int recordNumber, string joinDatabaseName, bool repost)
        {
            string deleteRecordCommand = getJoinDeleteRecordCommand(recordNumber, joinDatabaseName);
            if (deleteRecordCommand != null)
                return await deleteRecordDirect(deleteRecordCommand, repost);

            return null;
        }

        public async Task<HotRiotJSON> deleteRecordDirect(string deleteRecordCommand, bool repost)
        {
            if (repost == false)
                deleteRecordCommand = deleteRecordCommand + "&norepost=true";

            if( repost == true )
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
            if( isActionValid("userData" ) == false )
                return null;

            SubscriptionDetails subscriptionDetails = new SubscriptionDetails();

            subscriptionDetails.ServicePlan = getSubscriptionInfoString("servicePlan");
            subscriptionDetails.AccountStatus = getSubscriptionInfoString("accountStatus");

            if( subscriptionDetails.AccountStatus.Equals("Inactive") == false && subscriptionDetails.AccountStatus.Equals("Always Active") == false )
            {
                if( subscriptionDetails.AccountStatus.Equals("Active for a number of days") == true )
                    subscriptionDetails.RemainingDaysActive = getSubscriptionInfoInteger("remainingdaysActive");

                if( subscriptionDetails.AccountStatus.Equals("Active while account balance is positive") == true )
                {
                    subscriptionDetails.CurrentAccountBalance = getSubscriptionInfoString("currentAccountBalance");
                    subscriptionDetails.DailyRate = getSubscriptionInfoString("dailyRate");
                }
            }

            if( subscriptionDetails.AccountStatus.Equals("Inactive") == false )
            {
                subscriptionDetails.UsageRestrictions = getSubscriptionInfoString("usageRestrictions");
                if( subscriptionDetails.UsageRestrictions.Equals("By number of records") == true )
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
            if(paymentCount > 0 && paymentCount >= paymentNumber && paymentNumber >= 1)
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

            if(triggerDatabaseNames != null)
                for(var i=0; i<triggerDatabaseNames.Length; i++)
                    if(triggerDatabaseNames[i] == triggerDatabaseName)
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

        public void add( FieldInfo fieldInfo )
        {
            for(int i = 0; i < this.fieldInfo.Length; i++)
                if (this.fieldInfo[i] == null)
                {
                    this.fieldInfo[i] = fieldInfo;
                    break;
                }
        }

        public FieldInfo getFieldInfo( string fieldName )
        {
            for (int i = 0; i < this.fieldInfo.Length; i++)
                if (this.fieldInfo[i] != null)
                {
                    if( this.fieldInfo[i].FieldName.Equals( fieldName ) == true )
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
            get{ return recordCount; }
            set{ recordCount = value; }
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
}
