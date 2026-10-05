# Returns portal

Customers start a return in the web app (`web`). The API (`src/Returns.Api`) records it, and a small
Node service (`bff`) serves the web app's own pages. `src/Returns.AppHost` starts everything for
local development.
