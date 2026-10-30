using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoalShortagePortal.Application.Features.GenMUs.Helpers;
using CoalShortagePortal.Application.Features.GenMUs.Interfaces;
using CoalShortagePortal.Application.Features.GenMUs.Models;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace CoalShortagePortal.WebApp.Services.GenMUs.Implementations
{
    public class ReportingOracleService : IReportingOracleService
    {
        private readonly IConfiguration _configuration;

        public ReportingOracleService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<GeneratingStationOptionModel>>
            GetGeneratingStationsAsync()
        {
            var result = new List<GeneratingStationOptionModel>();

            var connectionString =
                _configuration.GetConnectionString("ReportingConnection");

            const string query = @"
                SELECT
                    gs.ID,
                    gs.GENERATING_STATION_NAME
                FROM REPORTING_WEB_UI_UAT.GENERATING_STATION gs
                WHERE gs.GENERATING_STATION_NAME IS NOT NULL
                ORDER BY gs.GENERATING_STATION_NAME";

            using (var connection = new OracleConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new GeneratingStationOptionModel
                        {
                            Id = Convert.ToInt32(reader["ID"]),
                            Name = reader["GENERATING_STATION_NAME"].ToString()
                        });
                    }
                }
            }

            return result;
        }

        public async Task<List<ScadaCellMappingModel>>
            GetScadaCellMappingsAsync()
        {
            var result = new List<ScadaCellMappingModel>();

            var connectionString =
                _configuration.GetConnectionString("ReportingConnection");

            const string query = @"
                SELECT
                    gs.ID,
                    gs.GENERATING_STATION_NAME,
                    gst.NAME AS STATION_TYPE_NAME,
                    gs.FUEL,
                    f.TYPE AS FUEL_TYPE,
                    scd.CELL_REFERENCE
                FROM REPORTING_WEB_UI_UAT.GENERATING_STATION gs
                INNER JOIN REPORTING_WEB_UI_UAT.SCADA_CONFIG_DETAILS scd
                    ON scd.ELMENT_ID = gs.ID
                LEFT JOIN REPORTING_WEB_UI_UAT.FUEL f
                    ON f.ID = gs.FUEL
                LEFT JOIN REPORTING_WEB_UI_UAT.GENERATING_STATION_TYPE gst
                    ON gst.ID = gs.STATION_TYPE
                WHERE scd.SCADA_FILE_NAME_ID = 7
                ORDER BY gs.GENERATING_STATION_NAME";

            using (var connection = new OracleConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new ScadaCellMappingModel
                            {
                                Id = reader["ID"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["ID"]),

                                GeneratingStationName =
                                    reader["GENERATING_STATION_NAME"] == DBNull.Value
                                        ? ""
                                        : reader["GENERATING_STATION_NAME"].ToString(),

                                StationTypeName =
                                    reader["STATION_TYPE_NAME"] == DBNull.Value
                                        ? ""
                                        : reader["STATION_TYPE_NAME"].ToString(),

                                Fuel = reader["FUEL"] == DBNull.Value
                                    ? null
                                    : Convert.ToInt32(reader["FUEL"]),

                                FuelType =
                                    reader["FUEL_TYPE"] == DBNull.Value
                                        ? ""
                                        : reader["FUEL_TYPE"].ToString(),

                                CellReference =
                                    reader["CELL_REFERENCE"] == DBNull.Value
                                        ? ""
                                        : reader["CELL_REFERENCE"].ToString()
                            });
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<StateOwnedStationModel>>
            GetStateOwnedStationsAsync(string state)
        {
            var stations = new List<StateOwnedStationModel>();

            if (string.IsNullOrWhiteSpace(state))
                return stations;

            var connectionString =
                _configuration.GetConnectionString("ReportingConnection");

            const string query = @"
                SELECT
                    ssm.FULL_NAME AS state_name,
                    owner_details.owners,
                    gs.GENERATING_STATION_NAME AS station_name,
                    cm.CLASSIFICATION
                FROM reporting_web_ui_uat.GENERATING_STATION gs
                LEFT JOIN reporting_web_ui_uat.CLASSIFICATION_MASTER cm
                    ON cm.ID = gs.CLASSIFICATION_ID
                LEFT JOIN reporting_web_ui_uat.SRLDC_STATE_MASTER ssm
                    ON ssm.ID = gs.LOCATION_ID
                LEFT JOIN
                (
                    SELECT
                        LISTAGG(own.owner_name, ',') WITHIN GROUP
                        (
                            ORDER BY own.owner_name
                        ) AS owners,
                        parent_entity_attribute_id AS element_id
                    FROM reporting_web_ui_uat.entity_entity_reln ent_reln
                    LEFT JOIN reporting_web_ui_uat.owner own
                        ON own.id = ent_reln.child_entity_attribute_id
                    WHERE ent_reln.child_entity = 'Owner'
                      AND ent_reln.parent_entity = 'GENERATING_STATION'
                      AND ent_reln.child_entity_attribute = 'OwnerId'
                      AND ent_reln.parent_entity_attribute = 'Owner'
                    GROUP BY parent_entity_attribute_id
                ) owner_details
                    ON owner_details.element_id = gs.ID
                WHERE UPPER(TRIM(ssm.FULL_NAME)) = :state_name
                  AND UPPER(TRIM(cm.CLASSIFICATION)) = 'STATE_OWNED'
                  AND gs.GENERATING_STATION_NAME IS NOT NULL
                ORDER BY gs.GENERATING_STATION_NAME";

            using (var connection = new OracleConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand(query, connection))
                {
                    command.BindByName = true;

                    command.Parameters.Add(
                        new OracleParameter(
                            "state_name",
                            state.Trim().ToUpperInvariant()
                        )
                    );

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            stations.Add(new StateOwnedStationModel
                            {
                                StateName =
                                    reader["STATE_NAME"] == DBNull.Value
                                        ? ""
                                        : reader["STATE_NAME"].ToString(),

                                Owners =
                                    reader["OWNERS"] == DBNull.Value
                                        ? ""
                                        : reader["OWNERS"].ToString(),

                                StationName =
                                    reader["STATION_NAME"] == DBNull.Value
                                        ? ""
                                        : reader["STATION_NAME"].ToString(),

                                Classification =
                                    reader["CLASSIFICATION"] == DBNull.Value
                                        ? ""
                                        : reader["CLASSIFICATION"].ToString()
                            });
                        }
                    }
                }
            }

            return stations
                .Where(x => !string.IsNullOrWhiteSpace(x.StationName))
                .GroupBy(x =>
                    StationNameHelper.NormalizeStationName(x.StationName))
                .Select(g => g.First())
                .ToList();
        }
    }
}