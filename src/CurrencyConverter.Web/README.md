# Currency Converter Web Application

A modern, TypeScript-based React application for currency conversion with a sleek FinTech-inspired design.

## Features

- 💱 **Currency Conversion** - Convert between popular currencies in real-time
- 📊 **Latest Exchange Rates** - View current exchange rates for all supported currencies
- 📈 **Historical Rates** - Track exchange rate trends over time with interactive charts
- 🔐 **JWT Authentication** - Secure login and registration
- 🎨 **FinTech Theme** - Modern dark theme with gradient accents
- 📱 **Responsive Design** - Works seamlessly on desktop and mobile devices

## Tech Stack

- **React 18** - UI library
- **TypeScript** - Type safety
- **Vite** - Fast build tool
- **React Router** - Navigation
- **Axios** - API communication
- **Recharts** - Data visualization
- **Lucide React** - Icon library
- **date-fns** - Date formatting

## Prerequisites

- Node.js 18+ and npm
- Currency Converter API running (default: http://localhost:8080)

## Getting Started

### 1. Install Dependencies

```bash
npm install
```

### 2. Configure Environment

Copy the example environment file and update the API URL if needed:

```bash
cp .env.example .env
```

Edit `.env`:
```
VITE_API_BASE_URL=http://localhost:8080
```

### 3. Start Development Server

```bash
npm run dev
```

The application will be available at: http://localhost:5173

### 4. Login

Use the default admin credentials (if you enabled `SeedDefaultAdmin` in the API):
- Email: `admin@admin.com`
- Password: `P@ssw0rd1234`

Or register a new account.

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

## Project Structure

```
src/
├── components/           # React components
│   ├── Login.tsx        # Login page
│   ├── Register.tsx     # Registration page
│   ├── Layout.tsx       # Main layout with navigation
│   ├── CurrencyConverter.tsx    # Currency conversion feature
│   ├── LatestRates.tsx          # Latest rates display
│   ├── HistoricalRates.tsx      # Historical rates with charts
│   └── ProtectedRoute.tsx       # Route protection
├── contexts/            # React contexts
│   └── AuthContext.tsx  # Authentication state
├── services/            # API services
│   └── api.ts          # API client and methods
├── App.tsx             # Main app component with routing
├── main.tsx            # Entry point
└── index.css           # Global styles
```

## API Integration

The application connects to the Currency Converter API for:
- User authentication (register, login)
- Currency conversion
- Exchange rate data (latest and historical)

API endpoints used:
- `POST /register` - User registration
- `POST /login` - User login
- `POST /api/currency/convert` - Convert currencies
- `GET /api/currency/latest/{base}` - Get latest rates
- `GET /api/currency/historical` - Get historical rates

## Building for Production

```bash
npm run build
```

The production-ready files will be in the `dist/` directory.

To preview the production build:
```bash
npm run preview
```

## Deployment

The built application can be deployed to any static hosting service:
- Vercel
- Netlify
- GitHub Pages
- AWS S3
- Azure Static Web Apps

Make sure to set the `VITE_API_BASE_URL` environment variable to point to your production API.

## License

[Your License Here]
