using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace SyncServer
{
   class Program
   {
      // Простой "хранилище" данных (в реальной системе — БД)
      private static readonly Dictionary<string, dynamic> DataStore = new Dictionary<string, dynamic>();

      static void Main()
      {
         HttpListener listener = new HttpListener();
         listener.Prefixes.Add("http://127.0.0.1:8080/");
         listener.Start();
         Console.WriteLine("Сервер запущен на http://127.0.0.1:8080/");
         try
         {
            while (true)
            {
               // Блокирующий вызов
               HttpListenerContext context = listener.GetContext(); 
               ProcessRequest(context);
            }
         }
         catch (Exception ex)
         {
            Console.WriteLine("Ошибка: {0}", ex.Message);
         }
         finally
         {
            listener.Stop();
         }
      }

      private static void ProcessRequest(HttpListenerContext context)
      {
         HttpListenerRequest request = context.Request;
         HttpListenerResponse response = context.Response;
         string responseString;
         int statusCode;
         try
         {
            if (request.HttpMethod == "GET")
            {
               responseString = HandleGet(request);
               statusCode = 200;
            }
            else if (request.HttpMethod == "POST")
            {
               responseString = HandlePost(request);
               statusCode = 201;
            }
            else if (request.HttpMethod == "PUT")
            {
               responseString = HandlePut(request);
               statusCode = 200;
            }
            else if (request.HttpMethod == "DELETE")
            {
               responseString = HandleDelete(request);
               statusCode = 200;
            }
            else
            {
               responseString = JsonConvert.SerializeObject(new
               {
                  error = "Метод не поддерживается",
                  supported = new[] { "GET", "POST", "PUT", "DELETE" }
               });
               statusCode = 405;
            }
         }
         catch (Exception ex)
         {
            responseString = JsonConvert.SerializeObject(new { error = ex.Message });
            statusCode = 500;
         }

         // Отправка ответа
         response.StatusCode = statusCode;
         response.ContentType = "application/json";
         byte[] buffer = Encoding.UTF8.GetBytes(responseString);
         response.ContentLength64 = buffer.Length;
         using (Stream output = response.OutputStream)
         {
            output.Write(buffer, 0, buffer.Length);
         }
      }

      private static string HandleGet(HttpListenerRequest request)
      {
         string path = request.Url.LocalPath.Trim('/');
         if (string.IsNullOrEmpty(path))
         {
            // Возврат всех записей
            return JsonConvert.SerializeObject(DataStore);
         }

         if (DataStore.ContainsKey(path))
         {
            // Возврат конкретной записи
            return JsonConvert.SerializeObject(new { id = path, data = DataStore[path] });
         }
         else
         {
            return JsonConvert.SerializeObject(new { error = "Ресурс не найден" });
         }
      }

      private static string HandlePost(HttpListenerRequest request)
      {
         string path = request.Url.LocalPath.Trim('/');
         if (string.IsNullOrEmpty(path))
            throw new Exception("ID не указан в URL");

         var json = new StreamReader(request.InputStream, request.ContentEncoding).ReadToEnd();
         dynamic data = JsonConvert.DeserializeObject(json);
         DataStore[path] = data;

         return JsonConvert.SerializeObject(new { id = path, data = data });
      }

      private static string HandlePut(HttpListenerRequest request)
      {
         var path = request.Url.LocalPath.Trim('/');
         if (string.IsNullOrEmpty(path))
            throw new Exception("ID не указан в URL");

         if (!DataStore.ContainsKey(path))
            throw new Exception("Ресурс не найден");

         string json = new StreamReader(request.InputStream, request.ContentEncoding).ReadToEnd();
         object data = JsonConvert.DeserializeObject(json);
         DataStore[path] = data;

         return JsonConvert.SerializeObject(new { id = path, data = data });
      }

      private static string HandleDelete(HttpListenerRequest request)
      {
         var path = request.Url.LocalPath.Trim('/');
         if (string.IsNullOrEmpty(path))
            throw new Exception("ID не указан в URL");

         if (!DataStore.ContainsKey(path))
            throw new Exception("Ресурс не найден");
         DataStore.Remove(path);

         return JsonConvert.SerializeObject(new { success = true });
      }
   }
}
