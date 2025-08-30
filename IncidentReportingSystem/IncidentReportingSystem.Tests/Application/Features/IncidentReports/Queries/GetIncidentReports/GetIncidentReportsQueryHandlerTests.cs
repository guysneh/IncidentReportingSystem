using FluentAssertions;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReports;
using IncidentReportingSystem.Application.Features.IncidentReports.Mappers;
using IncidentReportingSystem.Application.Persistence;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Tests.Helpers;
using Moq;

namespace IncidentReportingSystem.Tests.Application.Features.IncidentReports.Queries.GetIncidentReports
{
    public class GetIncidentReportsQueryHandlerTests
    {
        private readonly Mock<IIncidentReportRepository> _repositoryMock;
        private readonly GetIncidentReportsQueryHandler _handler;

        public GetIncidentReportsQueryHandlerTests()
        {
            _repositoryMock = TestMockFactory.CreateIncidentReportRepositoryMock();
            _handler = new GetIncidentReportsQueryHandler(_repositoryMock.Object);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_ShouldReturnReports_FromRepository()
        {
            var query = new GetIncidentReportsQuery(
                Status: null,
                Skip: 0,
                Take: 10,
                SortBy: IncidentSortField.Status,
                Direction: SortDirection.Asc
            );

            var expectedReports = new List<IncidentReport>
        {
            TestMockFactory.CreateIncidentReport(Guid.NewGuid(), "desc1", "Berlin"),
            TestMockFactory.CreateIncidentReport(Guid.NewGuid(), "desc2", "Hamburg")
        };

            _repositoryMock
                .Setup(r => r.GetPagedAsync(
                    null, 0, 10,
                    null, null, null, null, null,
                    IncidentSortField.Status, SortDirection.Asc,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<IncidentReport>(expectedReports, total: expectedReports.Count, skip: 0, take: 10));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.Total.Should().Be(expectedReports.Count);
            result.Items.Should().HaveCount(expectedReports.Count);
            result.Items.Should().BeEquivalentTo(expectedReports.Select(e => e.ToDto()));

            _repositoryMock.Verify(r => r.GetPagedAsync(
                null, 0, 10,
                null, null, null, null, null,
                IncidentSortField.Status, SortDirection.Asc,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_ShouldPassFiltersToRepository()
        {
            var query = new GetIncidentReportsQuery(
                Status: IncidentStatus.Closed,
                Skip: 5,
                Take: 5,
                Category: IncidentCategory.Security,
                Severity: IncidentSeverity.High,
                SearchText: "Berlin",
                ReportedAfter: new DateTime(2023, 1, 1),
                ReportedBefore: new DateTime(2023, 12, 31),
                SortBy: IncidentSortField.Status,
                Direction: SortDirection.Asc
            );

            var expectedReports = new List<IncidentReport>
    {
        TestMockFactory.CreateIncidentReport(Guid.NewGuid(), "Security issue", "Berlin")
    };

            _repositoryMock
                .Setup(r => r.GetPagedAsync(
                    IncidentStatus.Closed, 5, 5,
                    IncidentCategory.Security,
                    IncidentSeverity.High,
                    "Berlin",
                    new DateTime(2023, 1, 1),
                    new DateTime(2023, 12, 31),
                    IncidentSortField.Status, SortDirection.Asc,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<IncidentReport>(expectedReports, total: expectedReports.Count, skip: 5, take: 5));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.Total.Should().Be(expectedReports.Count);
            result.Items.Should().HaveCount(expectedReports.Count);
            result.Items.Should().BeEquivalentTo(expectedReports.Select(e => e.ToDto()));

            _repositoryMock.Verify(r => r.GetPagedAsync(
                IncidentStatus.Closed, 5, 5,
                IncidentCategory.Security,
                IncidentSeverity.High,
                "Berlin",
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 31),
                IncidentSortField.Status, SortDirection.Asc,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_ShouldReturnEmptyList_WhenNoReportsFound()
        {
            var query = new GetIncidentReportsQuery(
                SortBy: IncidentSortField.Status,
                Direction: SortDirection.Asc
            );

            _repositoryMock
                .Setup(r => r.GetPagedAsync(
                    null, 0, 50,
                    null, null, null, null, null,
                    IncidentSortField.Status, SortDirection.Asc,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<IncidentReport>(Array.Empty<IncidentReport>(), total: 0, skip: 0, take: 50));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.Total.Should().Be(0);
            result.Items.Should().BeEmpty(); 

            _repositoryMock.Verify(r => r.GetPagedAsync(
                null, 0, 50,
                null, null, null, null, null,
                IncidentSortField.Status, SortDirection.Asc,
                It.IsAny<CancellationToken>()), Times.Once);
        }

    }
}
