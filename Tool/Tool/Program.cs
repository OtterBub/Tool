using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

public class BOOL
{
	public bool Is = false;
}

public class DATA
{
	public String SignalName;

	// EX) Frequency[(String)"S32"][(String)"MAG"][(String)freq] = (Double)value
	public Dictionary<String, Dictionary<String, Dictionary<String, Double>>> Frequency
		= new Dictionary<String, Dictionary<String, Dictionary<String, Double>>>( );

	public DATA( String signalName )
	{
		SignalName = signalName;
	}
}

namespace Tool
{
	class Program
	{
		static void Main( string[] args )
		{
			new Program( );
		}

		Dictionary<String, DATA> DataDic = new Dictionary<String, DATA>( );
		List<String> LineList = new List<String>( );
		List<String> SignalNumList = new List<String>( );
		List<String> OtherSignalList = new List<String>( );

		String _commander;
		Thread _thread;

		bool _fileread = false;
		bool _dataStart = false;

		// FileRead
		FileStream FS;
		StreamReader SR;
		String filePath = "Result/";
		char[] mainDelimiter = { ',' };
		char[] stringDelemiter = { '_', '-' };
		int count = 0;

		public Program( )
		{
			String lFilename = "";

			_thread = new Thread( new ParameterizedThreadStart( FileLoad ) );

			_thread.Start( lFilename );

			while( _fileread == false ) ;

			while( true )
			{
				_commander = Console.ReadLine( );
				if( _commander == "exit" )
				{
					_thread.Abort( );
					break;
				}
				else
				{
					if( _fileread )
					{
						if( SR == null )
							SR = new StreamReader( FS );

						String[] splitResult;

						String readLineStr = SR.ReadLine( );

						if( readLineStr == null )
						{
							Console.WriteLine( "End" );
							break;
						}

						splitResult = readLineStr.Split( mainDelimiter );

						// Skip Global Info
						while( _dataStart == false )
						{
							if( splitResult[0] == "Parameter" )
							{
								_dataStart = true;
								break;
							}
							readLineStr = SR.ReadLine( );
							splitResult = readLineStr.Split( mainDelimiter );
						}

						if( splitResult[0] == "Parameter" ) // Parameter Split
						{
							Console.WriteLine( "Parameter" );

							int valueCount = 0;
							List<String> list = new List<String>( );

							FileStream tempFS = new FileStream( filePath + "FileInfoCount" + ".txt", FileMode.Create, FileAccess.ReadWrite );
							StreamWriter SW = new StreamWriter( tempFS );

							String otherSignalName = null;
							String currentSignalNum = null;
							String currentFreq = null;
							String currentSignalName = null;
							DATA currentDATA = null;

							for( int i = 1; i < splitResult.Length; ++i )
							{
								// data[data.Length - 1] SignalName
								// data[data.Length - 2] Frequency
								// data[1] otherSignalName
								// data[0] signalNum

								LineList.Add( splitResult[i] );

								String[] data = splitResult[i].Split( stringDelemiter );

								currentSignalName = data[data.Length - 1];
								currentFreq = data[data.Length - 2];
								currentSignalNum = data[0];
								otherSignalName = data[1];

								if( DataDic.TryGetValue( currentSignalName, out currentDATA ) == false )
								{

									currentDATA = new DATA( currentSignalName );
									DataDic.Add( currentSignalName, currentDATA );
								}

								if( currentDATA.Frequency.ContainsKey( currentSignalNum ) == false )
								{
									currentDATA.Frequency[currentSignalNum] = new Dictionary<String, Dictionary<String, Double>>( );
								}

								if( currentDATA.Frequency[currentSignalNum].ContainsKey( otherSignalName ) == false )
								{
									currentDATA.Frequency[currentSignalNum][otherSignalName] = new Dictionary<String, Double>( );
								}

								if( currentDATA.Frequency[currentSignalNum][otherSignalName].ContainsKey( currentFreq ) == false )
								{
									currentDATA.Frequency[currentSignalNum][otherSignalName][currentFreq] = 0.0;
								}
							}

							foreach( var dataItem in DataDic )
							{
								DATA data = dataItem.Value;
								SW.Write( data.SignalName + "\n" );

								foreach( var freq in data.Frequency )
								{
									SW.Write( freq.Key + ", " );
									foreach( var signalOtherName in freq.Value )
									{
										SW.Write( signalOtherName.Key + "-" );
										SW.Write( signalOtherName.Value.Values.Count + ", " );
										valueCount += signalOtherName.Value.Values.Count;
									}
									SW.Write( "\n" );
								}
								SW.Write( "\n" );
							}

							SW.Write( "LineList Count: " + LineList.Count + " / " + valueCount );

							SW.Close( );
							tempFS.Close( );
						}
						else
						{
							int stringCount = 1;
							String[] splitData = readLineStr.Split( mainDelimiter );
							String unitNum = splitData[0].Split( stringDelemiter )[1];

							for( int i = 1; i < splitData.Length - 1; ++i )
							{
								// data[data.Length - 1] SignalName
								// data[data.Length - 2] Frequency
								// data[1] otherSignalName
								// data[0] signalNum

								String[] dataSplit = LineList[i - 1].Split( stringDelemiter );

								String SignalName = dataSplit[dataSplit.Length - 1];
								String Freq = dataSplit[dataSplit.Length - 2];
								String OtherSignalName = dataSplit[1];
								String SignalNum = dataSplit[0];

								if( SignalNum == "PID" )
									continue;

								DataDic[SignalName].Frequency[SignalNum][OtherSignalName][Freq] = Convert.ToDouble( splitData[i] );
							}

							//splitData[stringCount++];

							foreach( var dataItem in DataDic )
							{
								DATA data = dataItem.Value;

								FileStream tempFS =
									new FileStream( filePath + data.SignalName + "-Unit" + unitNum + ".s4p",
										FileMode.Create, FileAccess.ReadWrite );

								StreamWriter SW = new StreamWriter( tempFS );

								SW.Write( "Freq," );

								Dictionary<String, List<Double>> freqResult = new Dictionary<String, List<Double>>( );

								foreach( var signalNum in dataItem.Value.Frequency )
								{
									
									SW.Write( signalNum.Key + "," );

									foreach( var OtherSignalName in signalNum.Value )
									{
										// Convert row to column ( frequency )
										foreach( var freq in OtherSignalName.Value )
										{
											List<Double> list = null;
											if( freqResult.TryGetValue( freq.Key, out list ) == false )
											{
												list = new List<Double>();
												freqResult[freq.Key] = list;
											}
											freqResult[freq.Key].Add( freq.Value );
										}
										break;
									}
								}
								SW.Write( "\n" );
								foreach( var result in freqResult )
								{
									SW.Write( result.Key + "," );
									foreach( var resultItem in result.Value )
									{
										SW.Write( resultItem + "," );
									}
									SW.Write( "\n" );
								}

								SW.Close( );
								tempFS.Close( );
							}

							Console.WriteLine( splitData[0] );
						}
					}
				}
			}
			Console.WriteLine( "" );
		}

		public void FileLoad( object filename )
		{
			FS = new FileStream( ( String )filename, FileMode.Open, FileAccess.Read );
			_fileread = true;
		}
	}
}
