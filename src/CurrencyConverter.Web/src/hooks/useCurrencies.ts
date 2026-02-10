import { useState, useEffect } from 'react';
import { currencyApi } from '../services/api';

interface UseCurrenciesResult {
  currencies: string[];
  loading: boolean;
  error: string;
}

/**
 * Custom hook to fetch all available currencies from the API
 * Fetches latest rates for USD and extracts all currency codes
 * Cached after first successful fetch
 */
export const useCurrencies = (): UseCurrenciesResult => {
  const [currencies, setCurrencies] = useState<string[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string>('');

  useEffect(() => {
    const fetchCurrencies = async () => {
      // Check if we already have currencies cached in sessionStorage
      const cachedCurrencies = sessionStorage.getItem('available_currencies');
      if (cachedCurrencies) {
        try {
          const parsed = JSON.parse(cachedCurrencies);
          setCurrencies(parsed);
          setLoading(false);
          return;
        } catch {
          // If parsing fails, continue with fetch
          sessionStorage.removeItem('available_currencies');
        }
      }

      // Fetch currencies from API
      try {
        setLoading(true);
        setError('');

        // Fetch latest rates for USD to get all available currencies
        const response = await currencyApi.getLatestRates('USD');

        // Extract all currency codes from the rates object
        const currencyCodes = Object.keys(response.rates);

        // Add USD (base currency) to the list
        const allCurrencies = ['USD', ...currencyCodes].sort();

        // Remove duplicates (just in case)
        const uniqueCurrencies = Array.from(new Set(allCurrencies));

        setCurrencies(uniqueCurrencies);

        // Cache the result in sessionStorage
        sessionStorage.setItem('available_currencies', JSON.stringify(uniqueCurrencies));
      } catch (err: any) {
        console.error('Failed to fetch currencies:', err);
        setError('Failed to load currencies. Using defaults.');

        // Fallback to popular currencies if API fails
        const fallbackCurrencies = [
          'USD', 'EUR', 'GBP', 'JPY', 'CAD', 'AUD', 'CHF', 'CNY', 'SEK', 'NZD',
        ];
        setCurrencies(fallbackCurrencies);
      } finally {
        setLoading(false);
      }
    };

    fetchCurrencies();
  }, []);

  return { currencies, loading, error };
};
