using System.Text;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DealEngine
{
    class Program
    {
        //COMMENTS FROM ABRAHAM
        //API call function, most of the function body was taken fron StackOverflow
        public static string APICall(string URL, string urlParameters)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URL);
             string dataObject = "";

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body.
                dataObject = response.Content.ReadAsStringAsync().Result;
                //Console.WriteLine("Key: {0}", dataObject.Substring(dataObject.IndexOf("\"Key\":")+7,7));
                return dataObject;
            }
            else   
                dataObject = "Error: " + response.StatusCode;
           client.Dispose();
           return dataObject;
            
        }

        //COMMENTS FROM ABRAHAM
        //This funtion reads the JSON and returns the desired attribute valie, I used as reference the Newtonsoft.JON Documentation to create this funtion
        public static string getJsonValue(string jsonBody, string attributeName){
            try{
                JsonTextReader reader = new JsonTextReader(new StringReader(jsonBody));
                while (reader.Read())
                {
                    if (reader.Value != null)
                    {    
                        if (reader.TokenType.ToString().Trim() =="PropertyName" && reader.Value.ToString() == attributeName){
                            reader.Read();
                            if (reader.Value != null)
                                return reader.Value.ToString();  
                        }
                    }
                }
                return "";
            }
            catch(Exception){return "";}
        }
        
        //This funtion will return the Column position based on the Header Name
        // Retuns -1 in case the Header name does not exists or an Exception is throw
        public static int getHeaderColumnByName(string[] headerList, string headerName){
            int count = 0;
            try{
                foreach(string name in headerList){
                    if(name.Replace("\"", string.Empty) == headerName)
                        return count;
                    count++;
                }
            }catch(Exception){return -1;}
            return -1;
        }

        //This funtion returns true in case that the provided LatLong already exists on the Cache, if not exists then returns false
        public static bool isLatLongExists(string LatLong, ArrayList cacheLatLong){
            try{
                foreach(string ll in cacheLatLong)
                    if(ll.Split(';')[0].ToString() == LatLong)
                        return true;
            }
            catch(Exception){return false;}
            return false;
        }

        //This funtion retunr all details related to this LatLong as a String
        public static string getCacheResult(string LatLong, ArrayList cacheLatLong){
             try{
                foreach(string ll in cacheLatLong)
                    if(ll.Split(';')[0].ToString() == LatLong)
                        return ll;
            }
            catch(Exception){return "";}
            return "";
        }


        // This is the main Class
        static void Main(string[] args)
        {
            //Var Declaration
            string headers;
            string line;
            String[] results;
            ArrayList cacheLatLong = new ArrayList();
            string cacheResultOrig = "";
            string cacheResultDest = "";

            int origLatitudeCol = -1;
            int origLongitudeCol = -1;
            int destLatitudeCol = -1;
            int destLongitudeCol = -1;
            int origNameCol = -1;
            int destNameCol = -1;

            string LocationKey = "";
            string CurrentCond = "";
            string jsonKey = "";
            string jsonCurrentCond = "";

            //Accuweather API URLs
            string urlGetLocation = "http://dataservice.accuweather.com";
            string urlGetLocParam = "/locations/v1/cities/geoposition/search?apikey=wGLh0W3qWWwqzSFGnIHfniFMruXw6W5X&language=es-mx&q={0}";
            string urlGetCurrentCondParams = "/currentconditions/v1/{0}?apikey=wGLh0W3qWWwqzSFGnIHfniFMruXw6W5X&language=es-mx";

            //Start reading CSV File located on the Projects Folder (DataSet)
            var fileStream = new FileStream(@"DataSet/challenge_dataset.csv", FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8)) {    
                //Read the first line of the CSV to get the Headers
                try{
                    headers = streamReader.ReadLine();
                    //Getting Each Column Position
                    if(headers != null){
                        origLatitudeCol = getHeaderColumnByName(headers.Split(','), "origin_latitude");
                        origLongitudeCol = getHeaderColumnByName(headers.Split(','), "origin_longitude");
                        destLatitudeCol = getHeaderColumnByName(headers.Split(','), "destination_latitude");
                        destLongitudeCol = getHeaderColumnByName(headers.Split(','), "destination_longitude");
                        origNameCol = getHeaderColumnByName(headers.Split(','), "origin_iata_code");
                        destNameCol = getHeaderColumnByName(headers.Split(','), "destination_iata_code");

                        if (origLatitudeCol == -1 || origLongitudeCol == -1 || destLatitudeCol == -1 || destLongitudeCol == -1)
                            throw new Exception("Column Name Not Found" + origLatitudeCol.ToString());
                    }
                    else
                        throw new Exception("No Headers found or File is Empty");

                }catch(Exception e)
                { System.Console.WriteLine("Error to read the File Headers: " + e.Message); return;}
                
                //Read all Data Rows from CSV
                while ((line = streamReader.ReadLine()) != null) {
                    try{
                        //line = streamReader.ReadLine();
                        results = line.Split(',');
                        //Adding Coordenates to the cacheArray (Origing Information)
                        if (!isLatLongExists("[" + results[origLatitudeCol] + "," + results[origLongitudeCol] + "]", cacheLatLong))
                        {   
                            jsonKey = APICall(urlGetLocation, urlGetLocParam.Replace("{0}",results[origLatitudeCol] + "," + results[origLongitudeCol]));
                            if(!jsonKey.Contains("Error")){
                                LocationKey = getJsonValue(jsonKey, "Key");
                                if (LocationKey != "")
                                {
                                    jsonCurrentCond = APICall(urlGetLocation, urlGetCurrentCondParams.Replace("{0}",LocationKey));
                                    CurrentCond = getJsonValue(jsonCurrentCond, "Value") + "º C, " + getJsonValue(jsonCurrentCond, "WeatherText");
                                    cacheResultOrig = "[" + results[origLatitudeCol] + "," + results[origLongitudeCol] + "];" + CurrentCond + ";"+ results[origNameCol].Replace("\"","");
                                    cacheLatLong.Add(cacheResultOrig);
                                }
                            }
                            else{
                                System.Console.WriteLine("API call problem, " + jsonKey);
                                cacheResultOrig = "";
                            }
                        }
                        else{
                            //If reuslts already exists on cache, then take results from cache array instead of calling API
                            cacheResultOrig = getCacheResult("[" + results[origLatitudeCol] + "," + results[origLongitudeCol] + "]", cacheLatLong);
                            if(cacheResultOrig == "")
                                System.Console.WriteLine("Cache Problem, LatLong not found in cache");
                        }
                        //Adding Coordenates to the cacheArray (Destination Information)
                        if (!isLatLongExists("[" + results[destLatitudeCol] + "," + results[destLongitudeCol] + "]", cacheLatLong))
                        {
                            jsonKey = APICall(urlGetLocation, urlGetLocParam.Replace("{0}", results[destLatitudeCol] + "," + results[destLongitudeCol]));
                            if(!jsonKey.Contains("Error")){
                                LocationKey = getJsonValue(jsonKey, "Key");
                                if (LocationKey != "")
                                {
                                    jsonCurrentCond = APICall(urlGetLocation, urlGetCurrentCondParams.Replace("{0}",LocationKey));
                                    CurrentCond = getJsonValue(jsonCurrentCond, "Value") + "º C, " + getJsonValue(jsonCurrentCond, "WeatherText");
                                    cacheResultDest = "[" + results[destLatitudeCol] + "," + results[destLongitudeCol] + "];"+ CurrentCond + ";" + results[destNameCol].Replace("\"","");
                                    cacheLatLong.Add(cacheResultDest);
                                }
                            }
                            else{
                                System.Console.WriteLine("API call problem, " + jsonKey);
                                cacheResultDest = "";
                            }
                        }
                        else {
                            //If reuslts already exists on cache, then take results from cache array instead of calling API
                            cacheResultDest = getCacheResult("[" + results[destLatitudeCol] + "," + results[destLongitudeCol] + "]", cacheLatLong);
                            if(cacheResultDest == "")
                                System.Console.WriteLine("Cache Problem, LatLong not found in cache");
                        }
                        
                        if (cacheResultOrig != "" && cacheResultDest != "")
                            System.Console.WriteLine("La temperatura y condiciones actuales en tu Destino ({0}) son: {1}, mientras en tu salida ({2}), son: {3}", cacheResultDest.Split(';')[2], cacheResultDest.Split(';')[1], cacheResultOrig.Split(';')[2], cacheResultOrig.Split(';')[1]);
                        else
                            System.Console.WriteLine("No se cuenta con informacion para las siguientes coordenadas: {0}", "[" + results[origLatitudeCol] + "," + results[origLongitudeCol] + "]");
                        }
                        catch(Exception e){
                            System.Console.WriteLine("Error al procesar una linea del archivo. Error: {0}", e.Message);
                        }
                }
            }
        }    
    }
}