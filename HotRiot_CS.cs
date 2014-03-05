// HMAC Authentication Info: http://jokecamp.wordpress.com/2012/10/21/examples-of-creating-base64-hashes-using-hmac-sha256-in-different-languages/
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
 
namespace HotRiot_CS
{
    public sealed class HotRiot : defines
    {
        private static HotRiot HRInstance = new HotRiot();

        private string fullyQualifiedHRDAURL;
        private string fullyQualifiedHRURL;
        private string jSessionID;
        private string hmKey;

        private HotRiot(){}

        private static HotRiot getHotRiotInstance
        {
            get
            {
                return HRInstance;
            }
        }

        private HotRiotJSON postLink(string link)
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

                requestStream = request.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Dispose();
                requestStream.Close();
                requestStream = null;

                webResponse = request.GetResponse();
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
            catch(Exception ex)
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

        private HotRiotJSON postRequest(string url, NameValueCollection nvc, NameValueCollection files)
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

                requestStream = httpWebRequest.GetRequestStream();
                byte[] boundarybytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "\r\n");
                string formdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";

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

                    foreach (string key in files.Keys)
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

                webResponse = httpWebRequest.GetResponse();
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
            catch(Exception ex)
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

        private bool isActionValid(HotRiotJSON jsonResponse, string validAction )
        {
            string action = getAction(jsonResponse);
            if(action != null && action.Equals(validAction) == true)
                    return true;

            return false;
        }

        private HotRiotJSON processResponse(string unprocessedJsonResponse)
        {
            HotRiotJSON hotriotJSON = new HotRiotJSON( JObject.Parse(unprocessedJsonResponse) );
            setSession(hotriotJSON);
            return hotriotJSON;
        }

        private void setSession(HotRiotJSON jsonResponse)
        {
            String sessionID = getGeneralInfoString( jsonResponse, "sessionID" );
            if (sessionID != null )
                jSessionID = ";jsessionid=" + sessionID;
        }

        private string getGeneralInfoString(HotRiotJSON jsonResponse, string field)
        {
            try
            {
                return processDataString(jsonResponse["generalInformation"][field].ToString());
            }
            catch (NullReferenceException doNothing) { }

            return null;
        }

        private string getSubscriptionInfoString(HotRiotJSON jsonResponse, string field)
        {
            try
            {
                return processDataString(jsonResponse["subscriptionDetails"][field].ToString());
            }
            catch (NullReferenceException doNothing) { }

            return null;
        }

        private int getSubscriptionInfoInteger(HotRiotJSON jsonResponse, string field)
        {
            try
            {
                return (int)jsonResponse["subscriptionDetails"][field];
            }
            catch (NullReferenceException doNothing) { }

            return 0;
        }

        private string getSubscriptionPaymentInfoString(HotRiotJSON jsonResponse, string field)
        {
            try
            {
                return processDataString(jsonResponse["subscriptionPaymentInfo"][field].ToString());
            }
            catch (NullReferenceException doNothing) { }

            return null;
        }

        private int getSubscriptionPaymentInfoInteger(HotRiotJSON jsonResponse, string field)
        {
            try
            {
                return (int)jsonResponse["subscriptionPaymentInfo"][field];
            }
            catch (NullReferenceException doNothing) { }

            return 0;
        }

        private bool getGeneralInfoBool(HotRiotJSON jsonResponse, string field)
        {
            try
            {
                return (bool)jsonResponse["generalInformation"][field];
            }
            catch (NullReferenceException doNothing) { }

            return false;
        }

        private int getGeneralInfoInteger(HotRiotJSON jsonResponse, string field)
        {
            try
            {
                return (int)jsonResponse["generalInformation"][field];
            }
            catch (NullReferenceException doNothing) { }

            return 0;
        }

        private string[] getGeneralInfoArray(HotRiotJSON jsonResponse, string field)
        {
            string[] retArray = null;

            try
            {
                string jsonField = getGeneralInfoString(jsonResponse, field);
                JArray fieldJArray = JArray.Parse(jsonField);

                retArray = new string[fieldJArray.Count];
                for(int i=0; i<fieldJArray.Count; i++)
                    retArray[i] = (String)fieldJArray[i];
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }
            catch (Exception doNothing) { }

            return retArray;
        }

        private string[] getGeneralInfoArray(HotRiotJSON jsonResponse, string field, int index)
        {
            string[] retArray = null;

            try
            {
                string jsonField = getGeneralInfoString(jsonResponse, field);
                JArray fieldJArray = JArray.Parse(jsonField);
                fieldJArray = JArray.Parse(fieldJArray[index].ToString());

                retArray = new string[fieldJArray.Count];
                for (int i = 0; i < fieldJArray.Count; i++)
                    retArray[i] = (String)fieldJArray[i];
            }
            catch (NullReferenceException doNothing) { }
            catch (ArgumentNullException doNothing) { }
            catch (Exception doNothing) { }

            return retArray;
        }

        private HotRiotJSON submitRecordCount(NameValueCollection recordCountObject, string sll)
        {
            recordCountObject.Add("hsp-initializepage", "hsp-json");
            recordCountObject.Add("hsp-action", "recordcount");
            recordCountObject.Add("hsp-sll", sll);
            recordCountObject.Add("sinceLastLogin", "false");
            return postRequest(fullyQualifiedHRURL, recordCountObject, null);
        }

        private bool isValidRecordNumber( int recordNumber, HotRiotJSON jsonResponse)
        {
            if( recordNumber > 0 )
                if (recordNumber <= getGeneralInfoInteger(jsonResponse, "recordCount") )
                    return true;

            return false;
        }

        private string getFieldDataString( int recordNumber, string dbFieldName, HotRiotJSON jsonResponse )
        {
            try
            {
                string finalRecordNumber = "record_" + recordNumber;
                return processDataString(jsonResponse["recordData"][finalRecordNumber]["fieldData"][dbFieldName].ToString());
            }
            catch (NullReferenceException doNothing) { }

            return null;
        }

        private string getRecordDataString(int recordNumber, string recordDataName, HotRiotJSON jsonResponse)
        {
            try
            {
                string finalRecordNumber = "record_" + recordNumber;
                return processDataString(jsonResponse["recordData"][finalRecordNumber][recordDataName].ToString());
            }
            catch (NullReferenceException doNothing) { }

            return null;
        }

        private string getSubscriptionPaymentInfoString(int recordNumber, string fieldName, HotRiotJSON jsonResponse)
        {
            try
            {
                string finalRecordNumber = "payment_" + recordNumber;
                return processDataString(jsonResponse["subscriptionPaymentInfo"][finalRecordNumber][fieldName].ToString());
            }
            catch (NullReferenceException doNothing) { }

            return null;
        }

        private string processDataString(string data)
        {
            if(data != null)
                if (data.Length == 0)
                    data = null;

            return data;
        }

        private FieldInfo getDatabaseFieldInfo(int recordNumber, string fieldName, string databaseName, HotRiotJSON jsonResponse)
        {
            string dbFieldName = databaseName + "::" + fieldName;

            string jFieldInfoString = null;
            FieldInfo recordInfo = null;
            if ((jFieldInfoString = getFieldDataString(recordNumber, dbFieldName, jsonResponse)) != null)
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

        private string getJoinRecordSystemFieldData(int recordNumber, string systemFieldName, string databaseName, HotRiotJSON jsonResponse)
        {
            string fieldData = null;

            string dbFieldName = databaseName + "::" + systemFieldName;

            if( isValidRecordNumber(recordNumber, jsonResponse) == true )
                fieldData = getFieldDataString(recordNumber, dbFieldName, jsonResponse);

            return fieldData;
        }

        private DatabaseRecord getTriggerRecordInfo(int recordNumber, string triggerDatabaseName, HotRiotJSON jsonResponse)
        {
            DatabaseRecord databaseRecord = null;

            if( isValidRecordNumber(recordNumber, jsonResponse) == true )
            {
                var triggerDatabaseFieldNames = getTriggerFieldNames(triggerDatabaseName, jsonResponse);
                if( triggerDatabaseFieldNames != null && triggerDatabaseFieldNames.Length > 0)
                {
                    databaseRecord = new DatabaseRecord( triggerDatabaseFieldNames.Length );

                    for(int i=0; i<triggerDatabaseFieldNames.Length; i++)
                        databaseRecord.add( getDatabaseFieldInfo(recordNumber, triggerDatabaseFieldNames[i], triggerDatabaseName, jsonResponse) );
                }
            }

            return databaseRecord;
        }

/********************************************* PUBLIC API *********************************************/

        // ------------------------------------ INITIALIZE HOTRIOT ------------------------------------
        public static HotRiot init(string appName)
        {
            HotRiot hotriot = HotRiot.getHotRiotInstance;

            hotriot.fullyQualifiedHRDAURL = defines.PROTOCOL + appName + ".k222.info/da";
            hotriot.fullyQualifiedHRURL = defines.PROTOCOL + appName + ".k222.info/process";

            return hotriot;
        }

        // ------------------------------------- CHECKING RESULTS -------------------------------------
        public int getResultCode(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoInteger(jsonResponse, "processingResultCode");
        }

        public ResultDetails getResultDetails(HotRiotJSON jsonResponse)
        {
            ResultDetails resultDetails = new ResultDetails();

            resultDetails.ResultText = getGeneralInfoString(jsonResponse, "processingResult");
            resultDetails.ResultMessage = getGeneralInfoString(jsonResponse, "processingResultMessage");
            resultDetails.ProcessingTimeStamp = getGeneralInfoString(jsonResponse, "timeStamp");

            return resultDetails;
        }

        // ------------------------------------- GETTING ACTION -------------------------------------
        public string getAction(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoString(jsonResponse, "action");
        }

        // ----------------------------------- ACTION OPERATIONS ------------------------------------
        public HotRiotJSON submitRecord(string databaseName, NameValueCollection recordData, NameValueCollection files)
        {
            recordData.Add("hsp-formname", databaseName);
            return postRequest(fullyQualifiedHRURL, recordData, files);
        }

        public HotRiotJSON submitUpdateRecord(string databaseName, string recordID, string updatePassword, NameValueCollection recordData, NameValueCollection files)
        {
            recordData.Add("hsp-formname", databaseName);
            recordData.Add("hsp-json", updatePassword);
            recordData.Add("hsp-recordID", recordID);
            return postRequest(fullyQualifiedHRURL, recordData, files);
        }

        public HotRiotJSON submitSearch(string searchName, NameValueCollection searchCriterion)
        {
            searchCriterion.Add("hsp-formname", searchName);
            return postRequest(fullyQualifiedHRURL, searchCriterion, null);
        }

        public HotRiotJSON submitLogin(string loginName, NameValueCollection loginCredentials)
        {
            loginCredentials.Add("hsp-formname", loginName);
            return postRequest(fullyQualifiedHRURL, loginCredentials, null);
        }

        public HotRiotJSON submitNotification(string databaseName, NameValueCollection notificationData)
        {
            notificationData.Add("hsp-formname", databaseName);
            notificationData.Add("hsp-rtninsert", "1");
            return postRequest(fullyQualifiedHRURL, notificationData, null);
        }

        public HotRiotJSON submitLostLoginLookup(string loginName, NameValueCollection loginLookupData)
        {
            loginLookupData.Add("hsp-formname", loginName);
            return postRequest(fullyQualifiedHRURL, loginLookupData, null);
        }

        public HotRiotJSON submitRecordCount(NameValueCollection recordCountObject)
        {
            return submitRecordCount(recordCountObject, "false");
        }

        public HotRiotJSON submitRecordCountSLL(NameValueCollection recordCountObject)
        {
            return submitRecordCount(recordCountObject, "true");
        }

        public HotRiotJSON logout(Dictionary<string, string> logoutOptions)
        {
            string callbackData = null;

            if (logoutOptions != null)
                if (logoutOptions.TryGetValue("hsp-callbackdata", out callbackData) == true)
                    callbackData = "&hsp-callbackdata=" + callbackData;

            return postLink(fullyQualifiedHRDAURL + "?hsp-logout=hsp-json" + callbackData);
        }

        // ------------------------------------- INSERT ACTION -------------------------------------
        public bool isUpdate(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoBool(jsonResponse, "isUpdate");
        }

        public string getInsertDatabaseName(HotRiotJSON jsonResponse)
        {
            return getDatabaseName(jsonResponse);
        }

        public string[] getInsertFieldNames(HotRiotJSON jsonResponse)
        {
            return getFieldNames(jsonResponse);
        }

        public DatabaseRecord getInsertData(HotRiotJSON jsonResponse)
        {
            return getRecord(1, jsonResponse);
        }

        public bool getUserInfo(HotRiotJSON jsonResponse)
        {
            String loggedInUserInfoLink = getGeneralInfoString(jsonResponse, "loggedInUserInfoLink");

            if( loggedInUserInfoLink != null )
            {
                postLink(loggedInUserInfoLink);
                return true;
            }

            return false;
        }

        public string getDatePosted(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoString(jsonResponse, "datePosted");
        }

        public string getCallbackData(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoString(jsonResponse, "userData");
        }

        // ------------------------------------- SEARCH ACTION -------------------------------------
        public string getSearchName(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoString(jsonResponse, "searchName");
        }

        public RecordCountDetails getRecordCountInfo(HotRiotJSON jsonResponse)
        {
            RecordCountDetails recordCountDetails = new RecordCountDetails();

            recordCountDetails.RecordCount = getGeneralInfoInteger(jsonResponse, "recordCount");
            recordCountDetails.PageCount = getGeneralInfoInteger(jsonResponse, "pageCount");
            recordCountDetails.PageNumber = getGeneralInfoInteger(jsonResponse, "pageNumber");
            recordCountDetails.TotalRecordsFound = getGeneralInfoInteger(jsonResponse, "totalRecordsFound");

            return recordCountDetails;
        }

        public string getDatabaseName(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoString(jsonResponse, "databaseName");
        }

        public string[] getJoinDatabaseNames(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoArray(jsonResponse, "join");
        }

        public string[] getFieldNames(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoArray(jsonResponse, "databaseFieldNames");
        }

        public string[] getJoinFieldNames(string joinDatabaseName, HotRiotJSON jsonResponse)
        {
            string[] joinFieldNames = null;
            string[] joinDatabaseNames = getJoinDatabaseNames(jsonResponse);

            if( joinDatabaseNames != null )
                for( var i=0; i<joinDatabaseNames.Length; i++ )
                    if (joinDatabaseNames[i] == joinDatabaseName)
                    {
                        joinFieldNames = getGeneralInfoArray(jsonResponse, "joinFieldNames", i);
                        break;
                    }

            return joinFieldNames;
        }

        public DatabaseRecord getRecord(int recordNumber, HotRiotJSON jsonResponse)
        {
            DatabaseRecord databaseRecord = null;

            if( isValidRecordNumber(recordNumber, jsonResponse) == true )
            {
                string databaseName = getDatabaseName(jsonResponse);
                string[] databaseFieldNames = getFieldNames(jsonResponse);

                if( databaseName != null )
                {
                    databaseRecord = new DatabaseRecord( databaseFieldNames.Length );

                    for(var i=0; i<databaseFieldNames.Length; i++)
                        databaseRecord.add( getDatabaseFieldInfo(recordNumber, databaseFieldNames[i], databaseName, jsonResponse) );
                }
            }

            return databaseRecord;
        }

        public DatabaseRecord getJoinRecord( int recordNumber, string joinDatabaseName, HotRiotJSON jsonResponse)
        {
            DatabaseRecord databaseRecord = null;

            if( isValidRecordNumber(recordNumber, jsonResponse) == true )
            {
                string[] joinDatabaseFieldNames = getJoinFieldNames(joinDatabaseName, jsonResponse);
                if( joinDatabaseFieldNames.Length > 0 )
                {
                    databaseRecord = new DatabaseRecord( joinDatabaseFieldNames.Length );

                    for (var i = 0; i < joinDatabaseFieldNames.Length; i++)
                        databaseRecord.add(getDatabaseFieldInfo(recordNumber, joinDatabaseFieldNames[i], joinDatabaseName, jsonResponse));
                }
            }

            return databaseRecord;
        }

        public HotRiotJSON getRecordDetails(int recordNumber, HotRiotJSON jsonResponse)
        {
            HotRiotJSON jsonRecordDetailsResponse = null;

            if( isValidRecordNumber(recordNumber, jsonResponse) == true )
            {
                string recordLink = getRecordDataString(recordNumber, "recordLink", jsonResponse);
                if(recordLink != null)
                    jsonRecordDetailsResponse = postLink(recordLink);
            }

            return jsonRecordDetailsResponse;
        }

        public HotRiotJSON sortSearchResults(string fieldName, HotRiotJSON jsonResponse)
        {
            return sortSearchResultsEx(null, fieldName, jsonResponse);
        }

        public HotRiotJSON sortSearchResultsEx(string databaseName, string fieldName, HotRiotJSON jsonResponse)
        {
            FieldInfo recordInfo;

            if( databaseName == null )
            {
                databaseName = getDatabaseName(jsonResponse);
                recordInfo = getDatabaseFieldInfo(1, fieldName, databaseName, jsonResponse);

                if(recordInfo == null || recordInfo.DataCount == 0)
                {
                    string[] joinDatabaseNames = getJoinDatabaseNames( jsonResponse );
                    if( joinDatabaseNames != null )
                        for(var i=0; i<joinDatabaseNames.Length; i++)
                        {
                            recordInfo = getDatabaseFieldInfo(1, fieldName, joinDatabaseNames[i], jsonResponse);
                            if(recordInfo != null && recordInfo.DataCount != 0)
                                break;
                        }
                }

                if(recordInfo == null || recordInfo.DataCount == 0)
                {
                    string[] triggerDatabaseNames = getTriggerDatabaseNames( jsonResponse );
                    if(triggerDatabaseNames != null)
                        for(var x=0; x<triggerDatabaseNames.Length; x++)
                        {
                            recordInfo = getDatabaseFieldInfo(1, fieldName, triggerDatabaseNames[x], jsonResponse);
                            if(recordInfo != null && recordInfo.DataCount != 0)
                                break;
                        }
                    }

                if(recordInfo != null && recordInfo.DataCount != 0)
                    return postLink(recordInfo.SortLink);
            }
            else
            {
                recordInfo = getDatabaseFieldInfo(1, fieldName, databaseName, jsonResponse);
                if(recordInfo != null && recordInfo.DataCount != 0)
                    postLink(recordInfo.SortLink);
            }
        
            return null;
        }

        public HotRiotJSON getNextPage(HotRiotJSON jsonResponse)
        {
            string nextPageLink = getGeneralInfoString(jsonResponse, "nextPageLinkURL");
            if (nextPageLink != null)
                return postLink(nextPageLink);

            return null;
        }

        public HotRiotJSON getPreviousPage(HotRiotJSON jsonResponse)
        {
            string nextPageLink = getGeneralInfoString(jsonResponse, "previousPageLinkURL");
            if (nextPageLink != null)
                return postLink(nextPageLink);

            return null;
        }

        public HotRiotJSON getFirstPage(HotRiotJSON jsonResponse)
        {
            string nextPageLink = getGeneralInfoString(jsonResponse, "firstPageLinkURL");
            if (nextPageLink != null)
                return postLink(nextPageLink);

            return null;
        }

        public bool moreRecords(HotRiotJSON jsonResponse)
        {
            int pageCount = getGeneralInfoInteger(jsonResponse, "pageCount");
            int pageNumber = getGeneralInfoInteger(jsonResponse, "pageNumber");

            if( pageNumber != 0 && pageCount != 0 && pageNumber < pageCount )
                return true;

            return false;
        }

        // public bool getUserInfo(HotRiotJSON jsonResponse) Implementation in Insert Action

        public string getDeleteRecordCommand(int recordNumber, HotRiotJSON jsonResponse)
        {
            if (isValidRecordNumber(recordNumber, jsonResponse) == true)
                return getRecordDataString(recordNumber, "deleteRecordLink", jsonResponse);

            return null;
        }

        public string getJoinDeleteRecordCommand(int recordNumber, string joinDatabaseName,  HotRiotJSON jsonResponse)
        {
            return getJoinRecordSystemFieldData(recordNumber, "hsp-deleteRecordLink", joinDatabaseName, jsonResponse);
        }

        public HotRiotJSON deleteRecord(int recordNumber, bool repost, HotRiotJSON jsonResponse)
        {
            string deleteRecordCommand = getDeleteRecordCommand(recordNumber, jsonResponse);
            if (deleteRecordCommand != null)
                return deleteRecordDirect(deleteRecordCommand, repost);

            return null;
        }

        public HotRiotJSON deleteJoinRecord(int recordNumber, string joinDatabaseName, bool repostSearch, HotRiotJSON jsonResponse)
        {
            string deleteRecordCommand = getJoinDeleteRecordCommand(recordNumber, joinDatabaseName, jsonResponse);
            if (deleteRecordCommand != null)
                return deleteRecordDirect(deleteRecordCommand, repostSearch);

            return null;
        }

        public HotRiotJSON deleteRecordDirect(string deleteRecordCommand, bool repostSearch)
        {
            if (repostSearch == false)
                deleteRecordCommand = deleteRecordCommand + "&norepost=true";

            return postLink(deleteRecordCommand);
        }

        public string getEditRecordPassword(int recordNumber, HotRiotJSON jsonResponse)
        {
            if (isValidRecordNumber(recordNumber, jsonResponse) == true)
                return getRecordDataString(recordNumber, "editRecordPswd", jsonResponse);

            return null;
        }

        public string getJoinEditRecordPassword(int recordNumber, string joinDatabaseName, HotRiotJSON jsonResponse)
        {
            return getJoinRecordSystemFieldData(recordNumber, "hsp-editRecordPswd", joinDatabaseName, jsonResponse);
        }

        public string getRecordID(int recordNumber, HotRiotJSON jsonResponse)
        {
            if (isValidRecordNumber(recordNumber, jsonResponse) == true)
                return getRecordDataString(recordNumber, "recordID", jsonResponse);

            return null;
        }

        public string getJoinRecordID(int recordNumber, string joinDatabaseName, HotRiotJSON jsonResponse)
        {
            return getJoinRecordSystemFieldData(recordNumber, "hsp-recordID", joinDatabaseName, jsonResponse);
        }

        public HotRiotJSON getJsonResponseFromRSL(string fieldName, HotRiotJSON jsonResponse)
        {
            return null;
        }

        public string getExcelDownloadLink(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoString(jsonResponse, "excelDownloadLink");
        }

        // public string getCallbackData(HotRiotJSON jsonResponse) Implementation in Insert Action

        // ------------------------------------- USER DATA ACTION -------------------------------------
        public string getRegDatabaseName(HotRiotJSON jsonResponse)
        {
            return getDatabaseName(jsonResponse);
        }

        public string[] getRegFieldNames(HotRiotJSON jsonResponse)
        {
            return getFieldNames(jsonResponse);
        }

        public DatabaseRecord getRegRecord(HotRiotJSON jsonResponse)
        {
            return getRecord(1, jsonResponse);
        }

        public string getLastLogin(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoString(jsonResponse, "lastLogin");
        }

        public SubscriptionInfo getSubscriptionInfo(HotRiotJSON jsonResponse)
        {
            SubscriptionInfo subscriptionInfo = new SubscriptionInfo();
            subscriptionInfo.LoggedInStatus = getGeneralInfoString(jsonResponse, "loggedInStatus");
            subscriptionInfo.SubscriptionStatus = getGeneralInfoString(jsonResponse, "subscriptionStatus");

            return subscriptionInfo;
        }

        public SubscriptionDetails getSubscriptionDetails(HotRiotJSON jsonResponse)
        {
            if( isActionValid( jsonResponse, "userData" ) == false )
                return null;

            SubscriptionDetails subscriptionDetails = new SubscriptionDetails();

            subscriptionDetails.ServicePlan = getSubscriptionInfoString(jsonResponse, "servicePlan");
            subscriptionDetails.AccountStatus = getSubscriptionInfoString(jsonResponse, "accountStatus");

            if( subscriptionDetails.AccountStatus.Equals("Inactive") == false && subscriptionDetails.AccountStatus.Equals("Always Active") == false )
            {
                if( subscriptionDetails.AccountStatus.Equals("Active for a number of days") == true )
                    subscriptionDetails.RemainingDaysActive = getSubscriptionInfoInteger(jsonResponse, "remainingdaysActive");

                if( subscriptionDetails.AccountStatus.Equals("Active while account balance is positive") == true )
                {
                    subscriptionDetails.CurrentAccountBalance = getSubscriptionInfoString(jsonResponse, "currentAccountBalance");
                    subscriptionDetails.DailyRate = getSubscriptionInfoString(jsonResponse, "dailyRate");
                }
            }

            if( subscriptionDetails.AccountStatus.Equals("Inactive") == false )
            {
                subscriptionDetails.UsageRestrictions = getSubscriptionInfoString(jsonResponse, "usageRestrictions");
                if( subscriptionDetails.UsageRestrictions.Equals("By number of records") == true )
                    subscriptionDetails.RecordStorageRestriction = getSubscriptionInfoString(jsonResponse, "recordStorageRestriction");
            }

            return subscriptionDetails;
        }

        public int getPaymentCount(HotRiotJSON jsonResponse)
        {
            return getSubscriptionPaymentInfoInteger(jsonResponse, "paymentCount");
        }

        public int getTotalPaid(HotRiotJSON jsonResponse)
        {
            return getSubscriptionPaymentInfoInteger(jsonResponse, "totalPaid");
        }

        public SubscriptionPaymentInfo getPaymentInfo(int paymentNumber, HotRiotJSON jsonResponse)
        {
            SubscriptionPaymentInfo subscriptionPaymentInfo = new SubscriptionPaymentInfo();

            subscriptionPaymentInfo.PaymentAmount = getSubscriptionPaymentInfoString(paymentNumber, "paymentAmount", jsonResponse);
            subscriptionPaymentInfo.ServicePlan = getSubscriptionPaymentInfoString(paymentNumber, "servicePlan", jsonResponse);
            subscriptionPaymentInfo.PaymentProcessor = getSubscriptionPaymentInfoString(paymentNumber, "paymentProcessor", jsonResponse);
            subscriptionPaymentInfo.TransactionID = getSubscriptionPaymentInfoString(paymentNumber, "transactionID", jsonResponse);
            subscriptionPaymentInfo.TransactionDate = getSubscriptionPaymentInfoString(paymentNumber, "transactionDate", jsonResponse);
            subscriptionPaymentInfo.Currency = getSubscriptionPaymentInfoString(paymentNumber, "currency", jsonResponse);

            return subscriptionPaymentInfo;
        }

        public string getEditRecordPassword(HotRiotJSON jsonResponse)
        {
            return getEditRecordPassword(1, jsonResponse);
        }

        public string getRecordID(HotRiotJSON jsonResponse)
        {
            return getRecordID(1, jsonResponse);
        }

        // ------------------------------------- RECORD DETAILS ACTION -------------------------------------
        // public string getDatabaseName(HotRiotJSON jsonResponse) Implementation in search action.

        // public string[] getFieldNames(HotRiotJSON jsonResponse) Implementation in search action.

        // public DatabaseRecord getRecord(int recordNumber, HotRiotJSON jsonResponse)  Implementation in search action.

        public string[] getTriggerDatabaseNames(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoArray(jsonResponse, "trigger");
        }

        public string[] getTriggerFieldNames(string triggerDatabaseName, HotRiotJSON jsonResponse)
        {
            string[] triggerFieldNames = null;
            string[] triggerDatabaseNames = getTriggerDatabaseNames(jsonResponse);

            if(triggerDatabaseNames != null)
                for(var i=0; i<triggerDatabaseNames.Length; i++)
                    if(triggerDatabaseNames[i] == triggerDatabaseName)
                    {
                        triggerFieldNames = getGeneralInfoArray(jsonResponse, "triggerFieldNames", i);
                        break;
                    }

            return triggerFieldNames;
        }

        public DatabaseRecord getTriggerRecord(string triggerDatabaseName, HotRiotJSON jsonResponse)
        {
            return getTriggerRecordInfo(1, triggerDatabaseName, jsonResponse);
        }

        // ------------------------------------- LOGIN ACTION -------------------------------------
        public string getLoginName(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoString(jsonResponse, "searchName");
        }

        public string getRegDatabaseName(HotRiotJSON jsonResponse)
        {
            return getDatabaseName(jsonResponse);
        }

        // public string[] getRegFieldNames(HotRiotJSON jsonResponse)  Implementation in user data action.

        // public string[] getRegRecords(HotRiotJSON jsonResponse)  Implementation in user data action.

        // public string[] getLastLogin(HotRiotJSON jsonResponse)  Implementation in user data action.

        // public bool getUserInfo(HotRiotJSON jsonResponse)  Implementation in insert action.

        // public string getEditRecordPassword(HotRiotJSON jsonResponse)  Implementation in user data action.

        // public string getRecordID(int recordNumber, HotRiotJSON jsonResponse) Implementation in search action.

        // public string[] getTriggerDatabaseNames(HotRiotJSON jsonResponse)  Implementation in record details action.

        // public string[] getTriggerFieldNames(string triggerDatabaseName, HotRiotJSON jsonResponse)  Implementation in record details action.

        // public DatabaseRecord getTriggerRecord(string triggerDatabaseName, HotRiotJSON jsonResponse)  Implementation in record details action.

        // public string getCallbackData(HotRiotJSON jsonResponse)  Implementation in insert action.

        // ------------------------------------- LOGOUT ACTION -------------------------------------
        // public string getCallbackData(HotRiotJSON jsonResponse)  Implementation in insert action.

        // ------------------------------------- GET LOGIN CREDENTIALS ACTION -------------------------------------
        // public string getLoginName(HotRiotJSON jsonResponse)  Implementation in login action.

        // public string getCallbackData(HotRiotJSON jsonResponse)  Implementation in insert action.

        // ------------------------------------- NOTIFICATION REGISTRATION ACTION -------------------------------------
        public string getNotificationDatabaseName(HotRiotJSON jsonResponse)
        {
            return getDatabaseName(jsonResponse);
        }

        public string[] getNotificationFieldNames(HotRiotJSON jsonResponse)
        {
            return getFieldNames(jsonResponse);
        }

        public DatabaseRecord getNotificationData(HotRiotJSON jsonResponse)
        {
            return getRecord(1, jsonResponse);
        }

        // public bool getUserInfo(HotRiotJSON jsonResponse)  Implementation in insert action.

        // public string getDatePosted(HotRiotJSON jsonResponse)   Implementation in insert action.

        // public string getCallbackData(HotRiotJSON jsonResponse)  Implementation in insert action.


        // ------------------------------------- RECORD COUNT ACTION -------------------------------------
        public string getRecordCountDatabaseName(HotRiotJSON jsonResponse)
        {
            return getDatabaseName(jsonResponse);
        }

        // public bool getUserInfo(HotRiotJSON jsonResponse)  Implementation in insert action.

        public int getRecordCount(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoInteger(jsonResponse, "recordCount");
        }

        public RecordCountParameters getOptionalRecordCountParameters(HotRiotJSON jsonResponse)
        {
            RecordCountParameters recordCountParameters = new RecordCountParameters();

            recordCountParameters.FieldName = getGeneralInfoString(jsonResponse, "fieldName");
            recordCountParameters.CountOperator = getGeneralInfoString(jsonResponse, "operator");
            recordCountParameters.Comparator = getGeneralInfoString(jsonResponse, "comparator");

            return recordCountParameters;
        }

        public string getSinceLastLoginFlag(HotRiotJSON jsonResponse)
        {
            return getGeneralInfoString(jsonResponse, "sll");
        }

        // public string getCallbackData(HotRiotJSON jsonResponse)  Implementation in insert action.

        // ------------------------------------- DELETE RECORD ACTION -------------------------------------
        // public string getDatabaseName(HotRiotJSON jsonResponse)  Implementation in search action.

        // public string getSearchName(HotRiotJSON jsonResponse)  Implementation in search action.

        // public string getRecordID(HotRiotJSON jsonResponse)  Implementation in user data action.


        /******************************************* END PUBLIC API *******************************************/

        static void Main(string[] args)
        {
            HotRiot hotriot = HotRiot.init( "acuclient" );

            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("fname", "Tom");
            nvc.Add("lname", "Smith"); 
            nvc.Add("address", "123 Center Street");
            nvc.Add("city", "Sempter");
            nvc.Add("state", "Fl.");
            nvc.Add("zipCode", "33324");
            nvc.Add("email", "ajserpicojunk@gmail.com");
            nvc.Add("phone", "800.818.1879");
            nvc.Add("password", "11111");

            NameValueCollection files = new NameValueCollection();
            files.Add("photo", "C:\\Users\\Anthony\\Pictures\\photo.JPG");
            try
            {
                ResultDetails resultDetails = null;

                HotRiotJSON hotriotJSON = hotriot.submitRecord("clientRegistration", nvc, files);
                if (hotriot.getResultCode(hotriotJSON) != 0)
                {
                    resultDetails = hotriot.getResultDetails(hotriotJSON);
                }
                else
                {
                    string action = hotriot.getAction(hotriotJSON);
                    bool isUpdate = hotriot.isUpdate(hotriotJSON);
                    string[] fieldNamesArray = hotriot.getInsertFieldNames(hotriotJSON);
                    DatabaseRecord dr = hotriot.getInsertData(hotriotJSON);
                    hotriot.getRecordDetails(1, hotriotJSON);  // Does not exist.

                    nvc.Clear();
                    nvc.Add("formFK", "6964550949438762531" );
                    HotRiotJSON hotriotJSONSearchResponse = hotriot.submitSearch("adminFormHeaderSearch", nvc);
                    DatabaseRecord joinDatabaseRecord = null;
                    if (hotriot.isValidRecordNumber(1, hotriotJSONSearchResponse) == true)
                        joinDatabaseRecord = hotriot.getJoinRecord(1, "formQuestion", hotriotJSONSearchResponse);
                    hotriot.sortSearchResults("formFK", hotriotJSONSearchResponse);  

                    action = hotriot.getAction(hotriotJSONSearchResponse);
                    isUpdate = hotriot.isUpdate(hotriotJSONSearchResponse);
                    string deleteRecordCommand = hotriot.getDeleteRecordCommand(1, hotriotJSONSearchResponse);
                    HotRiotJSON hotriotJSONRecordDetails = hotriot.getRecordDetails(1, hotriotJSONSearchResponse);
                }

                String userInfoLink = (String)((HotRiotJSON)hotriotJSON)["generalInformation"]["loggedInUserInfoLink"];
                hotriotJSON = hotriot.postLink(userInfoLink);
            }
            catch( HotRiotException hex )
            {
                Exception ie = hex.InnerException;
                ie = hex.InnerException;
            }
        }
    }

    interface defines
    {
        private static const string PROTOCOL = "https://";

        public static const int SUCCESS = 0;
        public static const int GENERAL_ERROR = -1;
        public static const int SUBSCRIPTION_RECORD_LIMIT_EXCEPTION = 1;
        public static const int INVALID_CAPTCHA_EXCEPTION = 2;
        public static const int INVALID_DATA_EXCEPTION = 3;
        public static const int NOT_UNIQUE_DATA_EXCEPTION = 4;
        public static const int ACCESS_DENIED_EXCEPTION = 5;
        public static const int FILE_SIZE_LIMIT_EXCEPTION = 6;
        public static const int DB_FULL_EXCEPTION = 7;
        public static const int BAD_OR_MISSING_ID_EXCEPTION = 8;
        public static const int NO_RECORDS_FOUND_EXCEPTION = 9;
        public static const int RECORD_NOT_FOUND_EXCEPTION = 10;
        public static const int SESSION_TIMEOUT_EXCEPTION = 11;
        public static const int UNAUTHORIZED_ACCESS_EXCEPTION = 12;
        public static const int LOGIN_CREDENTIALS_NOT_FOUND = 13;
        public static const int LOGIN_NOT_FOUND_EXCEPTION = 14;
        public static const int INVALID_EMAIL_ADDRESS_EXCEPTION = 15;
        public static const int MULTIPART_LIMIT_EXCEPTION = 16;
        public static const int IP_ADDRESS_INSERT_RESTRICTION = 17;
        public static const int INVALID_REQUEST = 18;
        public static const int ANONYMOUS_USER_EXCEPTION = 19;
        public static const int INVALID_UPDATE_CREDENTIALS = 20;
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
