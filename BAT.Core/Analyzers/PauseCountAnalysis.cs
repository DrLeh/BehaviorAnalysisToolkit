﻿using System;
using System.Collections.Generic;
using BAT.Core.Analyzers.Results;
using BAT.Core.Common;
using BAT.Core.Config;

namespace BAT.Core.Analyzers
{
    public class PauseCountAnalysis : IAnalyzer
    {
        /// <summary>
        /// Gets the header.
        /// </summary>
        /// <returns>The header.</returns>
        public string[] GetHeader() { return Constants.PAUSE_RESULT_HEADER; }

        /// <summary>
        /// Gets the header csv.
        /// </summary>
        /// <returns>The header csv.</returns>
        public string GetHeaderCsv() { return Constants.PAUSE_RESULT_HEADER_CSV; }

        /// <summary>
        /// Analyze the specified input and parameters.
        /// </summary>
        /// <returns>The analyze.</returns>
        /// <param name="input">Input.</param>
        /// <param name="parameters">Parameters.</param>
        public IEnumerable<ICsvWritable> Analyze(IEnumerable<SensorReading> input,
                                                IEnumerable<Parameter> parameters)
        {
            var results = new List<PauseResult>();
            foreach (var param in parameters)
            {
                var threshold = Double.Parse(param.GetClauseValue(CommandParameters.Threshold));
                var windowSize = Int32.Parse(param.GetClauseValue(CommandParameters.Window));

                var startTime = DateTime.Now;
                DateTime endTime;
                bool currentlyPaused = false;
                int startNo = 0, endNo, windowCount = 0, pauseCount = 0;
                decimal currentDuration = 0.0M, totalDuration = 0.0M;

                foreach (var record in input)
                {
                    var filterField = record.GetType().GetProperty(param.Field);
                    if (filterField == null) continue;

                    var filterValue = Double.Parse(filterField.GetValue(record, null).ToString());
                    if (record.HasValidAccelVector && filterValue < threshold)
                    {
                        // we've found a pause instance
                        if (!currentlyPaused)
                        {
                            // new pause instance ... log the starting details
                            startTime = record.Time.GetValueOrDefault();
                            startNo = record.RecordNum.Value;
                            currentlyPaused = true;
                        }
                        // whether starting a new pause or continuing an old, bump the pause counter
                        windowCount++;
                    }
                    else
                    {
                        // measurement denotes active movement ... reset pause details
                        currentlyPaused = false;
                        windowCount = 0;
                    }

                    // check to see if we've completed a pause window
                    if (currentlyPaused && windowCount == windowSize)
                    {
                        // log ending details
                        endTime = record.Time.Value;
                        endNo = record.RecordNum.Value;

                        // determine the current duration and add to our running total
                        currentDuration = (windowCount * Constants.SAMPLING_PERIOD_IN_MS) / 1000.0M;
                        totalDuration += currentDuration;

                        // output pause details to file
                        results.Add(new PauseResult
                        {
                            Start = startTime,
                            StartNum = startNo,
                            End = endTime,
                            EndNum = endNo,
                            Duration = currentDuration
                        });
                        pauseCount++;

                        // reset pause details
                        currentlyPaused = false;
                        windowCount = 0;
                    }
                }
            }

            return results;
        }
    }
}