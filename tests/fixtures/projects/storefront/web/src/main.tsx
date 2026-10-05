import { createRoot } from "react-dom/client";
import { App } from "./App";
import "./styles/app.scss";
import "./styles/legacy.css";

createRoot(document.getElementById("root")!).render(<App />);
