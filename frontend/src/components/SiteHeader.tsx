import { Link } from 'react-router-dom';

export function SiteHeader() {
  return (
    <header className="site-header">
      <Link to="/" className="brand">
        <span className="brand-mark" aria-hidden="true">
          🌀
        </span>
        <span className="brand-name">ScrumMaster</span>
      </Link>
    </header>
  );
}
