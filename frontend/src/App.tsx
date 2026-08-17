import { Route, Routes } from 'react-router-dom';
import './App.css';
import { CreateBoardPage } from './pages/CreateBoardPage';
import { JoinBoardPage } from './pages/JoinBoardPage';
import { BoardPage } from './pages/BoardPage';
import { AzureDevOpsConfigPage } from './pages/AzureDevOpsConfigPage';
import { SiteHeader } from './components/SiteHeader';

function App() {
  return (
    <div className="app-shell">
      <SiteHeader />
      <main className="app-main">
        <Routes>
          <Route path="/" element={<CreateBoardPage />} />
          <Route path="/join/:boardId" element={<JoinBoardPage />} />
          <Route path="/board/:boardId" element={<BoardPage />} />
          <Route path="/equipe/:areaPath/azure-devops" element={<AzureDevOpsConfigPage />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;
