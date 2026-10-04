# Retrieval evaluation: tags-exact

Today's tag-only matching: task words (and two-word phrases) that are exactly category values.

- **Date:** 2026-10-04 · **Commit:** 3010e51
- **Data:** seed (fictitious placeholder standards): 73 topics from 3 owners (standards `ed2dc83d51aa`, golden set `a437a7ce2d8a`)
- **Model:** none · **Ranked:** no (rank metrics don't apply)

## Summary

| Metric | Value |
|---|---|
| Tasks | 50 (48 with standards, 2 without) |
| Mean recall | 9% |
| Found every expected topic | 8% of tasks |
| Found at least one | 10% of tasks |
| Returned nothing | 41 of 50 tasks |
| Mean precision (tasks that returned something) | 15% |
| Mean topics returned | 1.8 |
| Forbidden topics returned | 0 |
| No-standard tasks answered with nothing | 2 of 2 |
| Category recall | 5% |
| Category precision | 44% |

## Tasks

| Task | Categories (expected → found) | Expected | Missed | Returned | Recall | Precision |
|---|---|---|---|---|---|---|
| Pick the colors for the new settings page | styling, design-tokens, ui → – | Colors | Colors | 0 | 0% | n/a |
| The XYZ product page needs a new button color *(product: xyz-public-app)* | styling, branding → – | Colors | Colors | 0 | 0% | n/a |
| Our Sass files still use @import, clean them up | sass → sass | Sass modules | – | 3 | 100% | 33% |
| Add a theme color variable to the SCSS | sass, design-tokens → – | Sass variables, Colors | Sass variables, Colors | 0 | 0% | n/a |
| This stylesheet has selectors nested six levels deep | sass, css → – | Nesting | Nesting | 0 | 0% | n/a |
| Make the dashboard work on phones | layout → – | Breakpoints | Breakpoints | 0 | 0% | n/a |
| Use consistent gaps between the cards | layout, styling → – | Spacing | Spacing | 0 | 0% | n/a |
| Set the body text size in the stylesheet | css, layout → – | Units | Units | 0 | 0% | n/a |
| Add a save button and a card to the profile page | ui → – | Components | Components | 0 | 0% | n/a |
| Build a sign-up form with validation messages | ux, ui → – | Forms, Error messages, Components | Forms, Error messages, Components | 0 | 0% | n/a |
| Show a spinner while the report loads | ux → – | Loading states | Loading states | 0 | 0% | n/a |
| What should we show users when the payment fails? | ux, content → – | Error messages | Error messages | 0 | 0% | n/a |
| Can a keyboard-only user reach every button on this page? | accessibility → – | Keyboard | Keyboard | 0 | 0% | n/a |
| Is light grey text on white readable enough? | accessibility, styling → – | Color contrast | Color contrast | 0 | 0% | n/a |
| Add an icon-only delete button | accessibility, ui → – | Text alternatives | Text alternatives | 0 | 0% | n/a |
| Write the copy for the homepage hero *(product: acme-website)* | website, content → – | Voice, Calls to action | Voice, Calls to action | 0 | 0% | n/a |
| Add a footer to the public site *(product: acme-website)* | website → – | Footer | Footer | 0 | 0% | n/a |
| Set up a new TypeScript project for the admin UI | frontend → ui | TypeScript | TypeScript | 13 | 0% | 0% |
| The page's JavaScript bundle is 3 MB | frontend, performance → – | Bundle budget | Bundle budget | 0 | 0% | n/a |
| Render user-written comments as HTML in the React app | react, security, input-validation → react | Untrusted HTML | – | 4 | 100% | 25% |
| Add Google Analytics to the checkout page | frontend, security → – | Third-party scripts, Content Security Policy | Third-party scripts, Content Security Policy | 0 | 0% | n/a |
| Which standards apply to a web API? | api, backend → api | Routes and versioning, Errors, Pagination, Authentication, Authorization, Input validation | – | 11 | 100% | 55% |
| Add a REST endpoint that returns a customer's orders | api, security → – | Routes and versioning, Pagination, Authorization | Routes and versioning, Pagination, Authorization | 0 | 0% | n/a |
| Return a proper error body when an order isn't found | api → – | Errors | Errors | 0 | 0% | n/a |
| Add retries to the outbound call to the pricing service | resilience → – | Retries, Timeouts | Retries, Timeouts | 0 | 0% | n/a |
| Calls to the inventory service sometimes hang forever | resilience → – | Timeouts | Timeouts | 0 | 0% | n/a |
| The XYZ app calls the pricing service, how long should it wait? *(product: xyz-public-app)* | resilience → – | Timeouts | Timeouts | 0 | 0% | n/a |
| The API is slow under load; requests block on database calls | performance, data-access → api | Async I/O, Queries | Queries | 11 | 50% | 9% |
| Compress large JSON responses | performance, api → – | Response compression | Response compression | 0 | 0% | n/a |
| Cache the product list *(product: xyz-public-app)* | caching → – | Caching | Caching | 0 | 0% | n/a |
| Which key should I use to cache customer profiles? | caching → – | Cache keys | Cache keys | 0 | 0% | n/a |
| Add a new column to the Orders table | data-access → – | Migrations | Migrations | 0 | 0% | n/a |
| This page loads every customer and then their orders one by one | data-access, performance → – | Queries | Queries | 0 | 0% | n/a |
| Where does the new service get its database connection string? | data-access, configuration → – | Connections | Connections | 0 | 0% | n/a |
| Read the feature flag settings for each environment | configuration → – | Settings, Environments | Settings, Environments | 0 | 0% | n/a |
| Store the database password for the new service | secrets, configuration → – | Secrets in source | Secrets in source | 0 | 0% | n/a |
| Validate the access token in the new API | authentication, api → api | Authentication | – | 11 | 100% | 9% |
| Make sure users can't see another customer's invoice | authorization → – | Authorization | Authorization | 0 | 0% | n/a |
| Users type a search term that goes into a SQL query | input-validation, data-access → – | Input validation, Output encoding, Queries | Input validation, Output encoding, Queries | 0 | 0% | n/a |
| A scanner flagged a vulnerable NuGet package | dependencies → – | Vulnerable packages | Vulnerable packages | 0 | 0% | n/a |
| Add a new package feed for an internal library | dependencies → – | Package sources | Package sources | 0 | 0% | n/a |
| Add logging to the payment workflow | observability, secrets → – | Logging, Sensitive data in logs | Logging, Sensitive data in logs | 0 | 0% | n/a |
| Kubernetes needs a liveness probe for the service | observability → – | Health endpoints | Health endpoints | 0 | 0% | n/a |
| Trace a slow request across services | observability, performance → – | Tracing | Tracing | 0 | 0% | n/a |
| Set up the test project for our .NET API *(runtime: dotnet)* | testing → api | Unit tests, Integration tests, Tests in .NET | Unit tests, Integration tests, Tests in .NET | 15 | 0% | 0% |
| npm audit reports a critical vulnerability in our Node API *(runtime: node)* | dependencies → node, api | Vulnerable packages, Node package audit | Vulnerable packages, Node package audit | 12 | 0% | 0% |
| Write tests for the discount calculator | testing → – | Unit tests | Unit tests | 0 | 0% | n/a |
| Test the API end to end with its database | testing → api | Integration tests | Integration tests | 11 | 0% | 0% |
| Tune the Kubernetes cluster's network policies | – → – | – | – | 0 | n/a | n/a |
| Train a model to forecast next month's sales | – → – | – | – | 0 | n/a | n/a |
