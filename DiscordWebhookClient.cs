using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using MimeDetective;

namespace ResoniteScreenshotExtensions;

public class DiscordWebhookClient
{
    private string _webhookUrl;
    private HttpClient _client;

    public DiscordWebhookClient(string webhookUrl)
    {
        _webhookUrl = webhookUrl;
        _client = new HttpClient();
    }

    public async Task<bool> SendImage(string srcPath, string? payloadJson = null)
    {
        using (var fileStream = File.OpenRead(srcPath))
        {
            var fileType = fileStream.GetFileType();
            fileStream.Seek(0, SeekOrigin.Begin);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(fileType.Mime);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = $"image.{fileType.Extension}",
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _webhookUrl);
            var form = new MultipartFormDataContent();
            form.Add(fileContent, "file");
            if (payloadJson != null)
            {
                form.Add(new StringContent(payloadJson), "payload_json");
            }
            request.Content = form;

            var response = await _client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }

    public class EmbedField
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsInlined { get; set; }

        public EmbedField(string name, string value, bool inlined = true)
        {
            Name = name;
            Value = value;
            IsInlined = inlined;
        }

        public string ToJson()
        {
            return $"{{\"name\": \"{Name}\", \"value\": \"{Value}\", \"inline\": {(IsInlined ? "true" : "false")} }}";
        }
    }

    public async Task<bool> SendImageWithMetadata(string srcPath, IEnumerable<EmbedField> fields)
    {
        var json = $"{{\"embeds\": [{{\"fields\": [{fields.Select(f => f.ToJson()).Aggregate((acc, curr) => $"{acc}, {curr}")}]}}]}}";
        return await SendImage(srcPath, json);
    }
}
