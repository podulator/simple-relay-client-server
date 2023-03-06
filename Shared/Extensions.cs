using System;
using System.IO;
using System.Threading.Tasks;

public static class StringExtensions {
	public static byte[] ToUTF8(this string input) {
		return System.Text.Encoding.Convert(System.Text.Encoding.Default, System.Text.Encoding.UTF8, System.Text.Encoding.Default.GetBytes(input));
	}
}

public static class StreamExtensions {

	private static readonly int HEADER_LENGTH = 4;

	public static async Task<byte[]> ReceiveRawMessage (this Stream stream) {
		int bytesRead = 0;
		int headerRead = 0;
		byte[] buffer = new byte[HEADER_LENGTH];

		// wait for the header tro arrive...
		while (headerRead < HEADER_LENGTH && (bytesRead = await stream.ReadAsync (buffer, headerRead, HEADER_LENGTH - headerRead).ConfigureAwait (false)) > 0) {
			headerRead += bytesRead;
		}

		if (headerRead < HEADER_LENGTH) return null;

		// read the header, and set that to the payload size to read
		int bytesRemaining = BitConverter.ToInt32 (buffer, 0);
		// allocate it
		byte[] data = new byte[bytesRemaining];

		while (bytesRemaining > 0 && (bytesRead = await stream.ReadAsync (data, data.Length - bytesRemaining, bytesRemaining)) != 0) {
			bytesRemaining -= bytesRead;
		}

		// something has gone wrong?
		if (bytesRemaining != 0) return null;
		return data;
	}

	public static Task SendRawMessage (this Stream stream, byte[] data) {
		return Task.WhenAll (
			stream.WriteAsync (BitConverter.GetBytes (data.Length), 0, HEADER_LENGTH),
			stream.WriteAsync (data, 0, data.Length)
		);
	}

}