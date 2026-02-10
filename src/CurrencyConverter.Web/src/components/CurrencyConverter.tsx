import React, { useState } from 'react';
import { currencyApi, type ConversionResponse } from '../services/api';
import { ArrowLeftRight, TrendingUp } from 'lucide-react';
import { useCurrencies } from '../hooks/useCurrencies';
import './CurrencyConverter.css';

const CurrencyConverter: React.FC = () => {
  const { currencies, loading: currenciesLoading } = useCurrencies();
  const [from, setFrom] = useState('USD');
  const [to, setTo] = useState('EUR');
  const [amount, setAmount] = useState<number>(100);
  const [result, setResult] = useState<ConversionResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleConvert = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const response = await currencyApi.convert({ from, to, amount });
      setResult(response);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Conversion failed. Please try again.');
      setResult(null);
    } finally {
      setLoading(false);
    }
  };

  const handleSwap = () => {
    setFrom(to);
    setTo(from);
    setResult(null);
  };

  return (
    <div className="converter-container">
      <div className="converter-card">
        <div className="converter-header">
          <TrendingUp className="converter-icon" />
          <h2>Currency Converter</h2>
          <p>Convert between currencies in real-time</p>
        </div>

        <form onSubmit={handleConvert} className="converter-form">
          {error && <div className="error-message">{error}</div>}

          <div className="currency-row">
            <div className="form-group">
              <label htmlFor="amount">Amount</label>
              <input
                type="number"
                id="amount"
                value={amount}
                onChange={(e) => setAmount(parseFloat(e.target.value) || 0)}
                min="0"
                step="0.01"
                required
                disabled={loading}
              />
            </div>

            <div className="form-group">
              <label htmlFor="from">From</label>
              <select
                id="from"
                value={from}
                onChange={(e) => setFrom(e.target.value)}
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

          <button type="button" className="btn-swap" onClick={handleSwap} disabled={loading}>
            <ArrowLeftRight size={20} />
          </button>

          <div className="currency-row">
            <div className="form-group">
              <label htmlFor="to">To</label>
              <select
                id="to"
                value={to}
                onChange={(e) => setTo(e.target.value)}
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

          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? 'Converting...' : 'Convert'}
          </button>
        </form>

        {result && (
          <div className="conversion-result">
            <div className="result-header">Conversion Result</div>
            <div className="result-amount">
              <span className="amount-from">
                {result.originalAmount} {result.fromCurrency}
              </span>
              <ArrowLeftRight className="result-arrow" />
              <span className="amount-to">
                {result.convertedAmount.toFixed(2)} {result.toCurrency}
              </span>
            </div>
            <div className="result-rate">
              Exchange Rate: 1 {result.fromCurrency} = {result.exchangeRate.toFixed(4)} {result.toCurrency}
            </div>
            <div className="result-timestamp">
              Updated: {new Date(result.date).toLocaleString()}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default CurrencyConverter;
