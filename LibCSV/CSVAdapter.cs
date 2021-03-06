using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using LibCSV.Dialects;
using LibCSV.Exceptions;

namespace LibCSV
{
	/// <summary>
	/// CSVAdapter class is advanced csv reader/writer that supports read/write of all records and transformations.
	/// </summary>
	public class CSVAdapter : IDisposable
	{
		private string _filename;
		
		private string _encoding;
		
		private TextReader _reader;
		
		private TextWriter _writer;
		
		private Dialect _dialect;
		
		private string[] _headers;

		public bool IsDisposed { get; private set; }

		public CSVAdapter(Dialect dialect, string filename, string encoding)
		{
			_dialect = dialect;
			_filename = filename;
			_encoding = encoding;
		}
		
		public CSVAdapter(Dialect dialect, TextReader reader)
		{
			_dialect = dialect;
			_reader = reader;
		}
		
		public CSVAdapter(Dialect dialect, TextWriter writer)
		{
			_dialect = dialect;
			_writer = writer;
		}
		
		public CSVAdapter(Dialect dialect, TextWriter writer, string[] headers)
		{
			_dialect = dialect;
			_writer = writer;
			_headers = headers;
			
			CheckHeaders();
		}
		
		private ICSVReader CreateReader()
		{
			return _reader == null ? _dialect.CreateReader(_filename, _encoding) : _dialect.CreateReader(_reader);
		}
		
		private ICSVWriter CreateWriter()
		{
			return _writer == null ? _dialect.CreateWriter(_filename, _encoding) : _dialect.CreateWriter(_writer);
		}
		
		private void CheckHeaders()
		{
			if (_dialect.HasHeader && (_headers == null || _headers.Length < 1))
			{
				throw new HeaderIsNullException("Provided header array is not initialized!");
			}
		}
		
		public IEnumerable ReadAll(IDataTransformer transformer)
		{
			IList results = new ArrayList();
			
			var reader = CreateReader();

			try
			{
				reader.Open();

				string[] aliases = null;
				if (_dialect.HasHeader)
				{
					aliases = reader.Headers;
					if (aliases == null)
					{
						throw new HeaderIsNullException("Failed to read headers");
					}
				}

				while (reader.Next())
				{
					results.Add(transformer.TransformTuple(reader.Current, aliases));
				}
			}
			finally
            {
				reader.Dispose();
			}

			return transformer.TransformResult(results);
		}

		public async Task<IEnumerable> ReadAllAsync(IDataTransformer transformer)
		{
			var results = new ArrayList();

			var reader = CreateReader();

			try
			{
				await reader.OpenAsync();

				string[] aliases = null;
				if (_dialect.HasHeader)
				{
					aliases = reader.Headers;
					if (aliases == null)
					{
						throw new HeaderIsNullException("Failed to read headers");
					}
				}

				while (await reader.NextAsync())
				{
					results.Add(transformer.TransformTuple(reader.Current, aliases));
				}
			} 
			finally
            {
				reader.Dispose();
			}

			return transformer.TransformResult(results);
		}

		public void WriteAll(IEnumerable data, IDataTransformer transformer)
		{
			if (transformer == null) 
			{
				throw new DataTransformerIsNullException();
			}

			var writer = CreateWriter();

			try
			{
				writer.Open();

				var cellCount = -1;

				if (_dialect.HasHeader && _headers != null && _headers.Length > 0)
				{
					writer.WriteRow(_headers);
				}

				foreach (var row in data)
				{
					var cells = transformer.TransformRow(row);
					if (cells != null)
					{
						if (cellCount == -1)
						{
							cellCount = cells.Length;
						}

						if (cells.Length != cellCount)
						{
							throw new NotEqualCellCountInRowsException(cells.Length, cellCount);
						}
					}

					writer.WriteRow(cells);
				}
			} 
			finally
            {
				writer.Dispose();
			}
		}

		public async Task WriteAllAsync(IEnumerable data, IDataTransformer transformer)
		{
			if (transformer == null)
			{
				throw new DataTransformerIsNullException();
			}

			var writer = CreateWriter();

			try
			{
				await writer.OpenAsync();

				var cellCount = -1;
				if (_dialect.HasHeader && _headers != null && _headers.Length > 0)
				{
					writer.WriteRow(_headers);
				}

				foreach (var row in data)
				{
					var cells = transformer.TransformRow(row);
					if (cells != null)
					{
						if (cellCount == -1)
						{
							cellCount = cells.Length;
						}

						if (cells.Length != cellCount)
						{
							throw new NotEqualCellCountInRowsException(cells.Length, cellCount);
						}
					}

					await writer.WriteRowAsync(cells);
				}
			}
			finally
			{
				writer.Dispose();
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
				}
				
				IsDisposed = true;
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		~CSVAdapter()
		{
			Dispose(false);
		}
	}
}

