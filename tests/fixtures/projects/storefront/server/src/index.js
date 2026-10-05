import express from "express";
import pg from "pg";

const pool = new pg.Pool({ connectionString: process.env.DATABASE_URL });
const app = express();

app.get("/products", async (_req, res) => {
  const { rows } = await pool.query("select id, name from products");
  res.json(rows);
});

app.listen(3000);
