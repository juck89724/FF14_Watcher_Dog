import React from 'react'
// @ts-ignore
window.Module = window.Module || {}; // Polyfill for PaddleLayout

import ReactDOM from 'react-dom/client'
import App from './App'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
