# Billing worker

A background worker that turns invoice events into invoice rows and a read model.

`Billing.AppHost` starts the worker with its dependencies for local development.
We may add Redis for rate limiting later; nothing in this project uses it yet.
