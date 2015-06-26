using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows.Forms;

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
		[STAThread]
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

			while( _fileread == false )
			{
				Console.WriteLine( "Input FileName" );

				lFilename = "";

				// key Input
				while(true)
				{
					ConsoleKeyInfo info = Console.ReadKey( true );
					
					// Ctrl + V ( Paste )
					if( ( ConsoleModifiers.Control & info.Modifiers ) != 0 )
					{
						if( Convert.ToInt32( info.KeyChar ) == 22 )
						{
							lFilename = Clipboard.GetText();
							Console.Write( Clipboard.GetText() + "\n" );
						}
					}
					else if ( info.Key == ConsoleKey.Enter ) // Press Enter
					{
						Console.WriteLine();
						break;
					}
					else
					{
						lFilename += info.KeyChar;
						Console.Write( info.KeyChar );
					}
				}

				CreateIfMissing( filePath );

				FileLoad( lFilename );
			}

			while( true )
			{
				//_commander = Console.ReadLine( );
				//if( _commander == "exit" )
				//{
				//	_thread.Abort( );
				//	break;
				//}
				//else
				{
					if( _fileread )
					{
						while( _fileread )
						{
							if( SR == null )
								SR = new StreamReader( FS );

							String[] splitResult;
							String readLineStr = SR.ReadLine( );

							if( readLineStr == null )
							{
								Console.WriteLine( "End Press Enter" );
								Console.ReadLine( );
								_fileread = false;
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

							// Get Paramater
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

										currentDATA.Frequency["S11"] = new Dictionary<String, Dictionary<String, Double>>( );
										currentDATA.Frequency["S12"] = new Dictionary<String, Dictionary<String, Double>>( );
										currentDATA.Frequency["S13"] = new Dictionary<String, Dictionary<String, Double>>( );
										currentDATA.Frequency["S21"] = new Dictionary<String, Dictionary<String, Double>>( );
										currentDATA.Frequency["S22"] = new Dictionary<String, Dictionary<String, Double>>( );
										currentDATA.Frequency["S23"] = new Dictionary<String, Dictionary<String, Double>>( );
										currentDATA.Frequency["S31"] = new Dictionary<String, Dictionary<String, Double>>( );
										currentDATA.Frequency["S32"] = new Dictionary<String, Dictionary<String, Double>>( );
										currentDATA.Frequency["S33"] = new Dictionary<String, Dictionary<String, Double>>( );

										foreach ( var freqItem in currentDATA.Frequency )
										{
											currentDATA.Frequency[freqItem.Key]["MAG"] = new Dictionary<String, Double>( );
											currentDATA.Frequency[freqItem.Key]["ANG"] = new Dictionary<String, Double>( );		
										}
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
										foreach ( var freqItem in currentDATA.Frequency )
										{
											currentDATA.Frequency[freqItem.Key]["MAG"][currentFreq] = 0.0;
											currentDATA.Frequency[freqItem.Key]["ANG"][currentFreq] = 0.0;		
										}
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

								SW.Write( "LineList Count: " + LineList.Count );

								SW.Close( );
								tempFS.Close( );
							}
							else
							{
								String[] splitData = readLineStr.Split( mainDelimiter );
								String unitNum = splitData[0];

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
										new FileStream( filePath + data.SignalName + "-" + unitNum + ".s4p",
											FileMode.Create, FileAccess.ReadWrite );

									StreamWriter SW = new StreamWriter( tempFS );
									SW.Write("#\tHZ\tS\tDB\tR50.0\n");
									SW.Write("!\t" + "{0:MM}/{0:dd}/{0:yyyy}\t" + "{1}" + "\n", DateTime.Now, DateTime.Now.ToString("hh:mm:ss tt", new CultureInfo("en-US")));
									SW.Write( "Freq\t" );

									// freqResult[Frequency]
									Dictionary<String, List<Double>> freqMAGResult = new Dictionary<String, List<Double>>( );
									Dictionary<String, List<Double>> freqANGResult = new Dictionary<String, List<Double>>( );

									foreach( var signalNum in dataItem.Value.Frequency )
									{

										SW.Write( signalNum.Key + "\t\t" );

										foreach( var OtherSignalName in signalNum.Value )
										{
											
											// Convert row to column ( frequency )
											if( OtherSignalName.Key == "MAG" )
											{
												foreach( var freq in OtherSignalName.Value )
												{
													List<Double> list = null;
													if( freqMAGResult.TryGetValue( freq.Key, out list ) == false )
													{
														list = new List<Double>( );
														freqMAGResult[freq.Key] = list;
													}
													freqMAGResult[freq.Key].Add( freq.Value );
												}
											}
											else
											{
												foreach( var freq in OtherSignalName.Value )
												{
													List<Double> list = null;
													if( freqANGResult.TryGetValue( freq.Key, out list ) == false )
													{
														list = new List<Double>( );
														freqANGResult[freq.Key] = list;
													}
													freqANGResult[freq.Key].Add( freq.Value );
												}
											}
										}
									}
									SW.Write( "\n" );
									// Write Frequency
									foreach( var result in freqMAGResult )
									{
										SW.Write( result.Key + "\t" );

										for( int i = 0; i < result.Value.Count; ++i )
										{
											SW.Write( result.Value[i] + "\t" );
											SW.Write( freqANGResult[result.Key][i] + "\t" );
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
					else // File dont Read
					{
						break;
					}
				}
			}
			Console.WriteLine( "" );
		}

		public void FileLoad( object filename )
		{
			try
			{
				FS = new FileStream( ( String )filename, FileMode.Open, FileAccess.Read );
				_fileread = true;
			}
			catch( Exception e )
			{
				Console.WriteLine( "Failed Open File Please Check FileName or FilePath" );
				_fileread = false;
			}
		}

		public void CreateIfMissing( String path )
		{
			bool folderExists = Directory.Exists( path );
			if( !folderExists )
				Directory.CreateDirectory( path );
		}
	}
}
