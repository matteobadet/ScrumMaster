import { Route, Routes } from 'react-router-dom';
import { CreateBoardPage } from './pages/CreateBoardPage';
import { BoardPage } from './pages/BoardPage';

function App() {
  return (
    <Routes>
      <Route path="/" element={<CreateBoardPage />} />
      <Route path="/board/:boardId" element={<BoardPage />} />
    </Routes>
  );
}

export default App;
