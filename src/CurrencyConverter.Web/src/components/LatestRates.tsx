import React, { useState, useEffect } from 'react';
import { currencyApi, type LatestRatesResponse } from '../services/api';
import { DollarSign, RefreshCw } from 'lucide-react';
import { useCurrencies } from '../hooks/useCurrencies';
import './LatestRates.css';

const LatestRates: React.FC = () => {
  const { currencies, loading: currenciesLoading } = useCurrencies();
  const [baseCurrency, setBaseCurrency] = useState('USD');
  const [rates, setRates] = useState<LatestRatesResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const fetchRates = async () => {
    setError('');
    setLoading(true);

    try {
      const response = await currencyApi.getLatestRates(baseCurrency);
      setRates(response);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to fetch rates. Please try again.');
      setRates(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRates();
  }, [baseCurrency]);

  const handleRefresh = () => {
    fetchRates();
  };

  return (
    <div className="rates-container">
      <div className="rates-card">
        <div className="rates-header">
          <DollarSign className="rates-icon" />
          <h2>Latest Exchange Rates</h2>
          <p>Real-time currency exchange rates</p>
        </div>

        <div className="rates-controls">
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

          <button
            className="btn-refresh"
            onClick={handleRefresh}
            disabled={loading}
            title="Refresh rates"
          >
            <RefreshCw className={loading ? 'spinning' : ''} />
            Refresh
          </button>
        </div>

        {error && <div className="error-message">{error}</div>}

        {rates && (
          <div className="rates-content">
            <div className="rates-info">
              <span className="rates-base">Base: {rates.baseCurrency}</span>
              <span className="rates-date">Date: {rates.date || 'N/A'}</span>
            </div>

            <div className="rates-grid">
              {Object.entries(rates.rates).map(([currency, rate]) => (
                <div key={currency} className="rate-card">
                  <div className="rate-currency">{currency}</div>
                  <div className="rate-value">{rate.toFixed(4)}</div>
                  <div className="rate-description">
                    1 {rates.baseCurrency} = {rate.toFixed(4)} {currency}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {loading && <div className="loading-spinner">Loading rates...</div>}
      </div>
    </div>
  );
};

export default LatestRates;
