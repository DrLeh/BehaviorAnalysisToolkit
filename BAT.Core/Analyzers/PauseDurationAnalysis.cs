﻿using System;
using System.Collections.Generic;
using BAT.Core.Analyzers.Results;
using BAT.Core.Common;
using BAT.Core.Config;

namespace BAT.Core.Analyzers
{
	public class PauseDurationAnalysis : IAnalyzer
	{
        decimal totalDuration = 0.0M;
        int totalPauseCount = 0;

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

                DateTime startTime = DateTime.Now, endTime = DateTime.Now;
                bool currentlyPaused = false, isValidWindow = false;
				int startNo = 0, endNo = 0, windowCount = 0;
                PauseResult result;

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
                            startTime = record.Time.Value;
                            startNo = record.RecordNum.Value;
                            currentlyPaused = true;
                        }

                        // whether starting a new pause or continuing an old,
                        // log the last sub-threshold values and bump the pause counter
                        endTime = record.Time.Value;
                        endNo = record.RecordNum.Value;
                        windowCount++;
                    }
                    else
                    {
						// check to see if we've completed a pause window
						isValidWindow = (windowCount >= windowSize);
			            result = verifyPause(currentlyPaused, isValidWindow, startTime,
                                                 startNo, endTime, endNo, windowCount);
                        if (result != null) results.Add(result);

                        // measurement denotes active movement ... reset pause details
                        currentlyPaused = false;
                        windowCount = 0;
                    }
				}

				// it's possible that the last record of the data set is a pause
				// (will not trigger automatically if there is no valid record following)
				result = verifyPause(currentlyPaused, isValidWindow, startTime,
										 startNo, endTime, endNo, windowCount);
				if (result != null) results.Add(result);
			}

			return results;
		}

        /// <summary>
        /// Verifies the pause.
        /// </summary>
        /// <param name="currentlyPaused">If set to <c>true</c> currently paused.</param>
        /// <param name="validWindow">If set to <c>true</c> valid window.</param>
        /// <param name="startTime">Start time.</param>
        /// <param name="startNo">Start no.</param>
        /// <param name="endTime">Last time.</param>
        /// <param name="endNo">Last no.</param>
        /// <param name="windowCount">Window count.</param>
        private PauseResult verifyPause(bool currentlyPaused, bool validWindow, DateTime startTime,
                                 int startNo, DateTime endTime, int endNo, int windowCount) {
	        if (currentlyPaused && validWindow) {
				// determine the current duration and add to our running total
				decimal currentDuration = (windowCount * Constants.SAMPLING_PERIOD_IN_MS) / 1000.0M;
                totalDuration += currentDuration;

				// output pause details to file
				totalPauseCount++;
				return (new PauseResult
				{
					Start = startTime,
					StartNum = startNo,
					End = endTime,
					EndNum = endNo,
					Duration = currentDuration
				});
	        }

            return null;
	    }
	}
}