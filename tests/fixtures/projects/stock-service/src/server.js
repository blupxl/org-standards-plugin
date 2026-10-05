import express from "express";
import { MongoClient } from "mongodb";
import Redis from "ioredis";

const mongo = new MongoClient(process.env.MONGO_URL);
const cache = new Redis(process.env.CACHE_URL);
const app = express();

app.get("/stock/:sku", async (req, res) => {
  const cached = await cache.get(req.params.sku);
  if (cached) return res.json(JSON.parse(cached));
  const item = await mongo.db("stock").collection("items").findOne({ sku: req.params.sku });
  res.json(item);
});

app.listen(3000);
