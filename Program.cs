using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace yournamespace
{
    class Program
    {
        //Parametros comunes de conexion
        private static string format = "XML"; //XML, JSON
        private static string userId = "{your user id here}";
        private static string apiKey = "{your apikey here}";
        private static string version = "1.0";
        //Hora de Perú GMT -5
        private static string timeStamp = DateTime.Now.ToString("yyyy-MM-dd") + DateTime.Now.ToString("THH") + DateTime.Now.ToString(":mm") + DateTime.Now.ToString(":ss") + "-05:00";
        
        //Para retornar objeto Encoding al generar hash
        private static readonly Encoding encoding = Encoding.UTF8;

        static void Main(string[] args)
        {
            //Mostrando ordenes del día
            Console.WriteLine("***** Mostrar ordenes del día *****");

            //Importante: No modificar el orden!
            string createdAfter = DateTime.UtcNow.ToString("yyyy-MM-dd");
            createdAfter += ("T00:00:00-05:00");
            string action = "GetOrders";

            //Construyendo cadena para encriptar y unir a url
            //Importante: No modificar el orden!
            string pars = "Action=" + action + "&CreatedAfter=" + createdAfter + "&Format=" + format + "&Timestamp=" + timeStamp + "&UserID=" + userId + "&Version=" + version;

            //Enviando los parametros a la funcion de conexion a linio
            sendStringRequestAsync(pars, apiKey).Wait();
        }

        //Para conexion a linio
        static async Task sendStringRequestAsync(string pars, string apiKey)
        {
            //Modificando caracteres especiales para url de api linio
            var escapedSignature = WebUtility.UrlEncode(pars);
            escapedSignature = Regex.Replace(escapedSignature, "(%[0-9a-f]{2})", c => c.Value.ToLowerInvariant());
            escapedSignature = Regex.Replace(escapedSignature, "(%3d|%3D)", c => "=");
            escapedSignature = Regex.Replace(escapedSignature, "(%26)", c => "&");

            //Generando hash
            var keyByte = encoding.GetBytes(apiKey);
            string generatedSignature;
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                hmacsha256.ComputeHash(encoding.GetBytes(escapedSignature));
                generatedSignature = ByteToString(hmacsha256.Hash).ToLower();
            }

            //Construyendo url para solicitud a linio
            var url = string.Format("https://sellercenter-api.linio.com.pe/?{0}&Signature={1}", pars, generatedSignature);

            try
            {
                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await client.GetAsync(url))
                using (HttpContent content = response.Content)
                {
                    //Realizando conexion a linio en hilo paralelo
                    dynamic result = await content.ReadAsStringAsync();
                    Console.WriteLine("\n.....Esperando respuesta de Linio Api......\n");
                    if (result.Contains("ErrorResponse"))
                        Console.WriteLine("¡¡¡¡¡Error de respuesta!!!!!\n");

                    Console.WriteLine(" ----- Inicio de respuesta ----- ");
                    Console.WriteLine(result);
                    Console.WriteLine(" ----- Fin de respuesta ----- ");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.ReadLine();
        }

        //Para convertir bytes en string
        static string ByteToString(byte[] buff)
        {
            string sbinary = "";
            for (int i = 0; i < buff.Length; i++)
                sbinary += buff[i].ToString("X2"); /* hex format */
            return sbinary;
        }
    }
}
