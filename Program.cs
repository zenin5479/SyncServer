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
         listener.Prefixes.Add("http://localhost:8080/");
         listener.Start();
         Console.WriteLine("Сервер запущен на http://localhost:8080/");
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
            switch (request.HttpMethod)
            {
               case "GET":
                  responseString = HandleGet(request);
                  statusCode = 200;
                  break;

               case "POST":
                  responseString = HandlePost(request);
                  statusCode = 201;
                  break;

               case "PUT":
                  responseString = HandlePut(request);
                  statusCode = 200;
                  break;

               case "DELETE":
                  responseString = HandleDelete(request);
                  statusCode = 200;
                  break;

               default:
                  responseString = JsonConvert.SerializeObject(new
                  {
                     error = "Метод не поддерживается",
                     supported = new[] { "GET", "POST", "PUT", "DELETE" }
                  });
                  statusCode = 405;
                  break;
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
         var buffer = Encoding.UTF8.GetBytes(responseString);
         response.ContentLength64 = buffer.Length;
         using (var output = response.OutputStream)
         {
            output.Write(buffer, 0, buffer.Length);
         }
      }

      private static string HandleGet(HttpListenerRequest request)
      {
         var path = request.Url.LocalPath.Trim('/');

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
         var path = request.Url.LocalPath.Trim('/');
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
