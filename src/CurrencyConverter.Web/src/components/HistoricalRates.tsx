import React, { useState } from 'react';
import { currencyApi } from '../services/api';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { Calendar } from 'lucide-react';
import { format, subDays } from 'date-fns';
import { useCurrencies } from '../hooks/useCurrencies';
import './HistoricalRates.css';

interface ChartData {
  date: string;
  rate: number;
}

const HistoricalRates: React.FC = () => {
  const { currencies, loading: currenciesLoading } = useCurrencies();
  const [baseCurrency, setBaseCurrency] = useState('USD');
  const [targetCurrency, setTargetCurrency] = useState('EUR');
  const [startDate, setStartDate] = useState(format(subDays(new Date(), 30), 'yyyy-MM-dd'));
  const [endDate, setEndDate] = useState(format(new Date(), 'yyyy-MM-dd'));
  const [historicalData, setHistoricalData] = useState<ChartData[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const fetchHistoricalData = async (page: number = currentPage) => {
    setError('');
    setLoading(true);

    try {
      const response = await currencyApi.getHistoricalRates({
        baseCurrency: baseCurrency,
        startDate,
        endDate,
        pageNumber: page,
        pageSize: pageSize,
      });

      // Transform the response to chart data
      // response.data.rates is { "2024-01-01": { "EUR": 0.85, "GBP": 0.73 }, ... }
      const chartData: ChartData[] = Object.entries(response.data.rates)
        .map(([date, currencyRates]) => ({
          date,
          rate: currencyRates[targetCurrency] || 0,
        }))
        .filter((item) => item.rate > 0) // Filter out missing rates
        .sort((a, b) => a.date.localeCompare(b.date)); // Sort by date

      if (chartData.length === 0) {
        setError(`No data available for ${targetCurrency} in the selected date range.`);
      }

      setHistoricalData(chartData);
      setTotalPages(response.pagination.totalPages);
      setTotalCount(response.pagination.totalCount);
      setCurrentPage(response.pagination.currentPage);
    } catch (err: any) {
      console.error('Historical rates error:', err);
      setError(err.response?.data?.message || err.message || 'Failed to fetch historical data. Please try again.');
      setHistoricalData([]);
    } finally {
      setLoading(false);
    }
  };

  const handleFetchHistorical = async (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1); // Reset to first page on new search
    fetchHistoricalData(1);
  };

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= totalPages) {
      fetchHistoricalData(newPage);
    }
  };

  const handlePageSizeChange = (newPageSize: number) => {
    setPageSize(newPageSize);
    setCurrentPage(1);
    // Will fetch on next search or can auto-fetch here
  };

  const handlePresetRange = (days: number) => {
    setStartDate(format(subDays(new Date(), days), 'yyyy-MM-dd'));
    setEndDate(format(new Date(), 'yyyy-MM-dd'));
  };

  return (
    <div className="historical-container">
      <div className="historical-card">
        <div className="historical-header">
          <Calendar className="historical-icon" />
          <h2>Historical Exchange Rates</h2>
          <p>Track currency trends over time</p>
        </div>

        <form onSubmit={handleFetchHistorical} className="historical-form">
          {error && <div className="error-message">{error}</div>}

          <div className="currency-selection">
            <div className="form-group">
              <label htmlFor="baseCurrency">Base Currency</label>
              <select
                id="baseCurrency"
                value={baseCurrency}
                onChange={(e) => setBaseCurrency(e.target.value)}
                disabled={loading || currenciesLoading}
              >
                {currenciesLoading ? (
                  <option>Loading...</option>
                ) : (
                  currencies.map((currency) => (
                    <option key={currency} value={currency}>
                      {currency}
                    </option>
                  ))
                )}
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="targetCurrency">Target Currency</label>
              <select
                id="targetCurrency"
                value={targetCurrency}
                onChange={(e) => setTargetCurrency(e.target.value)}
                disabled={loading || currenciesLoading}
              >
                {currenciesLoading ? (
                  <option>Loading...</option>
                ) : (
                  currencies.map((currency) => (
                    <option key={currency} value={currency}>
                      {currency}
                    </option>
                  ))
                )}
              </select>
            </div>
          </div>

          <div className="date-selection">
            <div className="form-group">
              <label htmlFor="startDate">Start Date</label>
              <input
                type="date"
                id="startDate"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                max={endDate}
                required
                disabled={loading}
              />
            </div>

            <div className="form-group">
              <label htmlFor="endDate">End Date</label>
              <input
                type="date"
                id="endDate"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                min={startDate}
                max={format(new Date(), 'yyyy-MM-dd')}
                required
                disabled={loading}
              />
            </div>
          </div>

          <div className="preset-buttons">
            <button type="button" onClick={() => handlePresetRange(7)} disabled={loading}>
              7 Days
            </button>
            <button type="button" onClick={() => handlePresetRange(30)} disabled={loading}>
              30 Days
            </button>
            <button type="button" onClick={() => handlePresetRange(90)} disabled={loading}>
              90 Days
            </button>
            <button type="button" onClick={() => handlePresetRange(365)} disabled={loading}>
              1 Year
            </button>
          </div>

          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? 'Loading...' : 'View Historical Data'}
          </button>
        </form>

        {historicalData.length > 0 && (
          <div className="chart-container">
            <h3>
              {baseCurrency}/{targetCurrency} Exchange Rate Trend
            </h3>
            <ResponsiveContainer width="100%" height={400}>
              <LineChart data={historicalData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#2d3748" />
                <XAxis
                  dataKey="date"
                  stroke="#a0aec0"
                  tick={{ fill: '#a0aec0' }}
                />
                <YAxis
                  stroke="#a0aec0"
                  tick={{ fill: '#a0aec0' }}
                  domain={['auto', 'auto']}
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: '#1a202c',
                    border: '1px solid #2d3748',
                    borderRadius: '8px',
                    color: '#fff',
                  }}
                  labelStyle={{ color: '#a0aec0' }}
                />
                <Legend wrapperStyle={{ color: '#a0aec0' }} />
                <Line
                  type="monotone"
                  dataKey="rate"
                  stroke="#4299e1"
                  strokeWidth={2}
                  dot={{ fill: '#4299e1', r: 4 }}
                  activeDot={{ r: 6 }}
                  name={`Rate (${baseCurrency}/${targetCurrency})`}
                />
              </LineChart>
            </ResponsiveContainer>

            <div className="stats-summary">
              <div className="stat-card">
                <div className="stat-label">Average Rate</div>
                <div className="stat-value">
                  {(
                    historicalData.reduce((sum, d) => sum + d.rate, 0) / historicalData.length
                  ).toFixed(4)}
                </div>
              </div>
              <div className="stat-card">
                <div className="stat-label">Highest Rate</div>
                <div className="stat-value">
                  {Math.max(...historicalData.map((d) => d.rate)).toFixed(4)}
                </div>
              </div>
              <div className="stat-card">
                <div className="stat-label">Lowest Rate</div>
                <div className="stat-value">
                  {Math.min(...historicalData.map((d) => d.rate)).toFixed(4)}
                </div>
              </div>
            </div>

            {/* Data Table with Pagination */}
            <div className="data-table-container">
              <div className="table-header">
                <h4>Rate Details</h4>
                <div className="page-size-selector">
                  <label>Show:</label>
                  <select
                    value={pageSize}
                    onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                    disabled={loading}
                  >
                    <option value={10}>10</option>
                    <option value={20}>20</option>
                    <option value={50}>50</option>
                    <option value={100}>100</option>
                  </select>
                  <span>per page</span>
                </div>
              </div>

              <table className="historical-table">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Rate</th>
                    <th>Description</th>
                  </tr>
                </thead>
                <tbody>
                  {historicalData.map((item, index) => (
                    <tr key={index}>
                      <td>{item.date}</td>
                      <td className="rate-value">{item.rate.toFixed(4)}</td>
                      <td className="rate-description">
                        1 {baseCurrency} = {item.rate.toFixed(4)} {targetCurrency}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              {/* Pagination Controls */}
              {totalPages > 1 && (
                <div className="pagination-container">
                  <div className="pagination-info">
                    Showing page {currentPage} of {totalPages} ({totalCount} total days)
                  </div>
                  <div className="pagination-controls">
                    <button
                      className="btn-pagination"
                      onClick={() => handlePageChange(1)}
                      disabled={currentPage === 1 || loading}
                      title="First page"
                    >
                      «
                    </button>
                    <button
                      className="btn-pagination"
                      onClick={() => handlePageChange(currentPage - 1)}
                      disabled={currentPage === 1 || loading}
                      title="Previous page"
                    >
                      ‹
                    </button>

                    {/* Page numbers */}
                    {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                      let pageNum;
                      if (totalPages <= 5) {
                        pageNum = i + 1;
                      } else if (currentPage <= 3) {
                        pageNum = i + 1;
                      } else if (currentPage >= totalPages - 2) {
                        pageNum = totalPages - 4 + i;
                      } else {
                        pageNum = currentPage - 2 + i;
                      }
                      return (
                        <button
                          key={pageNum}
                          className={`btn-pagination ${currentPage === pageNum ? 'active' : ''}`}
                          onClick={() => handlePageChange(pageNum)}
                          disabled={loading}
                        >
                          {pageNum}
                        </button>
                      );
                    })}

                    <button
                      className="btn-pagination"
                      onClick={() => handlePageChange(currentPage + 1)}
                      disabled={currentPage === totalPages || loading}
                      title="Next page"
                    >
                      ›
                    </button>
                    <button
                      className="btn-pagination"
                      onClick={() => handlePageChange(totalPages)}
                      disabled={currentPage === totalPages || loading}
                      title="Last page"
                    >
                      »
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default HistoricalRates;
