using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Microservices.IsIdentifiable.Failures;

namespace Microservices.IsIdentifiable.Rules
{
    public class SocketRule : ICustomRule,IDisposable
    {
        public string Host { get; set; }
        public int Port { get; set; }
        
        private TcpClient _tcp;
        private NetworkStream _stream;
        private StreamWriter _write;
        private StreamReader _read;

        public RuleAction Apply(string fieldName, string fieldValue, out IEnumerable<FailurePart> badParts)
        {
            if (_stream == null)
            {
                _tcp = new TcpClient(Host, Port);
                _stream = _tcp.GetStream();
                _write = new StreamWriter(_stream);
                _read = new StreamReader(_stream);
            }

            // Translate the passed message into ASCII and store it as a Byte array.
            _write.Write(fieldValue);

            var responseData = _read.ReadToEnd();
            
            badParts = HandleResponse(responseData).ToArray();
            
            return badParts.Any() ? RuleAction.Report : RuleAction.None;
        }

        public IEnumerable<FailurePart> HandleResponse(string responseData)
        {
            int parts = 3;
            if (string.Equals(responseData, "\0") || string.IsNullOrWhiteSpace(responseData))
                yield break;

            if (responseData.Contains("\0\0")) 
                throw new Exception("Invalid sequence detected: two null terminators in a row");

            var result = responseData.Split("\0",StringSplitOptions.RemoveEmptyEntries);
            
            if(result.Length % parts != 0)
                throw new Exception($"Expected tokens to arrive in multiples of {parts} (but got '{result.Length}').  Full message was '{responseData}' (expected <classification><offset> or <null terminator>)");

            for (int i = 0; i < result.Length; i+=parts)
            {
                object c;
                if (!Enum.TryParse(typeof(FailureClassification), result[i],true, out c))
                    throw new Exception($"Could not parse TCP client classification '{result[i]}' (expected a member of Enum FailureClassification)");
                var classification = (FailureClassification)c;

                int offset;
                if(!int.TryParse(result[i+1],out offset))
                    throw new Exception($"Failed to parse offset from TCP client response.  Response was '{result[i+1]}' (expected int)");

                string badWord = result[i+2];

                yield return new FailurePart(badWord,classification,offset);
            }
        }

        public void Dispose()
        {
            _tcp?.Dispose();
            _stream?.Dispose();
        }
    }
}
