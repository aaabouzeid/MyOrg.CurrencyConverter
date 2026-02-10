import React from 'react';
import { Link, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { ArrowLeftRight, TrendingUp, Calendar, LogOut, User } from 'lucide-react';
import './Layout.css';

const Layout: React.FC = () => {
  const { isAuthenticated, userEmail, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="layout">
      <nav className="navbar">
        <div className="navbar-brand">
          <TrendingUp className="brand-icon" />
          <span className="brand-name">Currency Converter</span>
        </div>

        {isAuthenticated && (
          <>
            <div className="navbar-menu">
              <Link to="/" className="nav-link">
                <ArrowLeftRight size={18} />
                <span>Convert</span>
              </Link>
              <Link to="/latest-rates" className="nav-link">
                <TrendingUp size={18} />
                <span>Latest Rates</span>
              </Link>
              <Link to="/historical-rates" className="nav-link">
                <Calendar size={18} />
                <span>Historical</span>
              </Link>
            </div>

            <div className="navbar-user">
              <div className="user-info">
                <User size={18} />
                <span className="user-email">{userEmail}</span>
              </div>
              <button onClick={handleLogout} className="btn-logout">
                <LogOut size={18} />
                <span>Logout</span>
              </button>
            </div>
          </>
        )}
      </nav>

      <main className="main-content">
        <Outlet />
      </main>

      <footer className="footer">
        <p>&copy; {new Date().getFullYear()} Currency Converter. All rights reserved.</p>
        <p className="footer-tagline">Powered by real-time exchange rate data</p>
      </footer>
    </div>
  );
};

export default Layout;
