import express from "express";
import pgp from "pg-promise";

const db = pgp()(process.env.DATABASE_URL);
const app = express();

app.get("/pages/:slug", async (req, res) => {
  res.json(await db.oneOrNone("select body from pages where slug = $1", req.params.slug));
});

app.listen(3000);
