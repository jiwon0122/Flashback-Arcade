import MainLayout from './layouts/main/MainLayout';
import { UserProvider } from './contexts/UserContext';
import AuthRoutes from './components/AuthRoutes';
import { ToastContainer, toast } from 'react-toastify';
import MusicButton from './components/MusicButton';
import { Suspense, useEffect, useState } from 'react';
import CustomPacmanLoader from './components/PacmanLoader';

function App() {
  const [loading, setLoading] = useState(false);
  useEffect(() => {
    setLoading(true);
    setTimeout(() => {
      setLoading(false);
    }, 3000);
  }, []);

  return (
    <UserProvider>
      {loading ? (
        <div className="loading-animation">
          <CustomPacmanLoader className="" />
          <p className="loader1" style={{ color: "#f1e702" }}>Welcome to Flashback Arcade🔥</p>
        </div>
      ) : (
        <Suspense
          fallback={
            <div className="loading-animation">
              <CustomPacmanLoader />
            </div>
          }
        >
          <MainLayout>
            <AuthRoutes />
            <ToastContainer />
            <MusicButton />
          </MainLayout>
        </Suspense>
      )}
    </UserProvider>
  );
}

export default App;
