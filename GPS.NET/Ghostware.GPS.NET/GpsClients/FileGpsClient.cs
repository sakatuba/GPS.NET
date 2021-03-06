﻿using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Ghostware.GPS.NET.Enums;
using Ghostware.GPS.NET.Exceptions;
using Ghostware.GPS.NET.Models.ConnectionInfo;
using Ghostware.GPS.NET.Models.Events;
using Ghostware.GPS.NET.Models.GpsdModels;
using Ghostware.GPS.NET.Parsers;
using Ghostware.NMEAParser;
using Ghostware.NMEAParser.NMEAMessages;

namespace Ghostware.GPS.NET.GpsClients
{
    public class FileGpsClient : BaseGpsClient
    {
        #region Constructors

        public FileGpsClient(FileGpsInfo connectionData) : base(GpsType.File, connectionData)
        {
        }

        public FileGpsClient(BaseGpsInfo connectionData) : base(GpsType.File, connectionData)
        {
        }

        #endregion

        #region Connect and Disconnect

        public override bool Connect()
        {
            var data = (FileGpsInfo)GpsInfo;

            IsRunning = true;
            OnGpsStatusChanged(GpsStatus.Connecting);

            var parser = new NmeaParser();
            var gpsdDataParser = new GpsdDataParser();

            var headerLine = true;
            var latColumnIndex = 0;
            var longColumnIndex = 0;
            var minArraySize = 0;

            try
            {
                using (var streamReader = File.OpenText(data.FilePath))
                {
                    string line;
                    OnGpsStatusChanged(GpsStatus.Connected);
                    while (IsRunning && (line = streamReader.ReadLine()) != null)
                    {
                        OnRawGpsDataReceived(line);
                        switch (data.FileType)
                        {
                            case FileType.Nmea:
                                var nmeaResult = parser.Parse(line);
                                var result = nmeaResult as GprmcMessage;
                                if (result == null) continue;
                                OnGpsDataReceived(new GpsDataEventArgs(result));
                                break;
                            case FileType.Gpsd:
                                var gpsdResult = gpsdDataParser.GetGpsData(line);
                                var gpsLocation = gpsdResult as GpsLocation;
                                if (gpsLocation == null) continue;
                                OnGpsDataReceived(new GpsDataEventArgs(gpsLocation));
                                break;
                            case FileType.LatitudeLongitude:
                                if (headerLine)
                                {
                                    var headers = line.Split(';');

                                    for (var i = 0; i < headers.Length; i++)
                                    {
                                        if (headers[i] == Properties.Settings.Default.File_Latitude_Header)
                                        {
                                            latColumnIndex = i;
                                        }
                                        if (headers[i] == Properties.Settings.Default.File_Longitude_Header)
                                        {
                                            longColumnIndex = i;
                                        }
                                    }
                                    minArraySize = Math.Max(latColumnIndex, longColumnIndex);
                                    headerLine = false;
                                }
                                else
                                {
                                    var latLongResult = line.Split(';');
                                    if (latLongResult.Length < 2)
                                    {
                                        throw new InvalidFileFormatException(data.FilePath);
                                    }
                                    if (latLongResult.Length < minArraySize)
                                    {
                                        continue;
                                    }

                                    double latitude;
                                    if (
                                        !double.TryParse(latLongResult[latColumnIndex], NumberStyles.Any,
                                            CultureInfo.InvariantCulture, out latitude))
                                    {
                                        continue;
                                    }
                                    double longitude;
                                    if (
                                        !double.TryParse(latLongResult[longColumnIndex], NumberStyles.Any,
                                            CultureInfo.InvariantCulture, out longitude))
                                    {
                                        continue;
                                    }

                                    var message = new GpsDataEventArgs(latitude, longitude);
                                    OnGpsDataReceived(message);
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        Thread.Sleep(data.ReadFrequenty);
                    }
                    Disconnect();
                    return true;
                }
            }
            catch
            {
                Disconnect();
                throw;
            }

        }

        public override bool Disconnect()
        {
            IsRunning = false;
            OnGpsStatusChanged(GpsStatus.Disabled);
            return true;
        }

        #endregion
    }
}