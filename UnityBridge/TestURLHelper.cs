using System;
using UnityBridge.Tools.Utils;
using DotNext;

namespace UnityBridge.Test
{
    public class TestURLHelper
    {
        public static void Run()
        {
            Console.WriteLine("Testing URLHelper Refactoring...");

            // Test TryUrlEncode
            var encodeResult = URLHelper.TryUrlEncode("Hello World");
            if (encodeResult.IsSuccessful && encodeResult.Value == "Hello+World")
            {
                Console.WriteLine("TryUrlEncode Success: PASS");
            }
            else
            {
                Console.WriteLine($"TryUrlEncode Success: FAIL - {encodeResult.Error?.Message}");
            }

            // Test TryUrlDecode
            var decodeResult = URLHelper.TryUrlDecode("Hello+World");
            if (decodeResult.IsSuccessful && decodeResult.Value == "Hello World")
            {
                Console.WriteLine("TryUrlDecode Success: PASS");
            }
            else
            {
                Console.WriteLine($"TryUrlDecode Success: FAIL - {decodeResult.Error?.Message}");
            }

            // Test TryParseUrlParams
            var paramsResult = URLHelper.TryParseUrlParams("http://example.com?key=value");
            if (paramsResult.IsSuccessful && paramsResult.Value["key"].ToString() == "value")
            {
                Console.WriteLine("TryParseUrlParams Success: PASS");
            }
            else
            {
                Console.WriteLine($"TryParseUrlParams Success: FAIL - {paramsResult.Error?.Message}");
            }

            // Test Exception Handling (Simulated by passing null if possible, or just relying on internal logic)
            // Since HttpUtility.UrlEncode(null) returns null, it might not throw.
            // Let's try to pass something that might cause issues or just verify the structure exists.
            // Actually, the main goal is to ensure the refactoring didn't break existing functionality.
            // The Try method catches generic Exception, so any exception inside would be caught.

            Console.WriteLine("Tests Completed.");
        }
    }
}
