# Retrieval evaluation: tags-suggest

Today's tag-only matching: task words that are category values, plus the gateway's first "did you mean" suggestion for the rest.

- **Date:** 2026-10-03 · **Commit:** c667422
- **Data:** seed (fictitious placeholder standards): 60 topics from 3 owners (standards `edb230d305d1`, golden set `33bd42062bb0`)
- **Model:** none · **Ranked:** no (rank metrics don't apply)

## Summary

| Metric | Value |
|---|---|
| Tasks | 47 (45 with standards, 2 without) |
| Mean recall | 14% |
| Found every expected topic | 13% of tasks |
| Found at least one | 16% of tasks |
| Returned nothing | 30 of 47 tasks |
| Mean precision (tasks that returned something) | 6% |
| Mean topics returned | 3.1 |
| Forbidden topics returned | 0 |
| No-standard tasks answered with nothing | 1 of 2 |
| Category recall | 10% |
| Category precision | 25% |

## Tasks

| Task | Categories (expected → found) | Expected | Missed | Returned | Recall | Precision |
|---|---|---|---|---|---|---|
| Pick the colors for the new settings page | styling, design-tokens, ui → – | Colors | Colors | 0 | 0% | n/a |
| The XYZ product page needs a new button color *(product: xyz-public-app)* | styling, branding → – | Colors | Colors | 0 | 0% | n/a |
| Our Sass files still use @import, clean them up | sass → sass | Sass modules | – | 3 | 100% | 33% |
| Add a theme color variable to the SCSS | sass, design-tokens → css | Sass variables, Colors | – | 8 | 100% | 25% |
| This stylesheet has selectors nested six levels deep | sass, css → – | Nesting | Nesting | 0 | 0% | n/a |
| Make the dashboard work on phones | layout → – | Breakpoints | Breakpoints | 0 | 0% | n/a |
| Use consistent gaps between the cards | layout, styling → api | Spacing | Spacing | 11 | 0% | 0% |
| Set the body text size in the stylesheet | css, layout → – | Units | Units | 0 | 0% | n/a |
| Add a save button and a card to the profile page | ui → sass | Components | Components | 3 | 0% | 0% |
| Build a sign-up form with validation messages | ux, ui → ui, performance, input-validation | Forms, Error messages, Components | – | 24 | 100% | 13% |
| Show a spinner while the report loads | ux → – | Loading states | Loading states | 0 | 0% | n/a |
| What should we show users when the payment fails? | ux, content → – | Error messages | Error messages | 0 | 0% | n/a |
| Can a keyboard-only user reach every button on this page? | accessibility → react | Keyboard | Keyboard | 1 | 0% | 0% |
| Is light grey text on white readable enough? | accessibility, styling → – | Color contrast | Color contrast | 0 | 0% | n/a |
| Add an icon-only delete button | accessibility, ui → – | Text alternatives | Text alternatives | 0 | 0% | n/a |
| Write the copy for the homepage hero *(product: acme-website)* | website, content → – | Voice, Calls to action | Voice, Calls to action | 0 | 0% | n/a |
| Add a footer to the public site *(product: acme-website)* | website → website | Footer | – | 8 | 100% | 13% |
| Set up a new TypeScript project for the admin UI | frontend → ui, api | TypeScript | TypeScript | 24 | 0% | 0% |
| The page's JavaScript bundle is 3 MB | frontend, performance → css | Bundle budget | Bundle budget | 8 | 0% | 0% |
| Render user-written comments as HTML in the React app | react, security, input-validation → react | Untrusted HTML | Untrusted HTML | 1 | 0% | 0% |
| Add Google Analytics to the checkout page | frontend, security → – | Third-party scripts, Content Security Policy | Third-party scripts, Content Security Policy | 0 | 0% | n/a |
| Add a REST endpoint that returns a customer's orders | api, security → react | Routes and versioning, Pagination, Authorization | Routes and versioning, Pagination, Authorization | 1 | 0% | 0% |
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
| Where does the new service get its database connection string? | data-access, configuration → styling | Connections | Connections | 8 | 0% | 0% |
| Read the feature flag settings for each environment | configuration → react | Settings, Environments | Settings, Environments | 1 | 0% | 0% |
| Store the database password for the new service | secrets, configuration → – | Secrets in source | Secrets in source | 0 | 0% | n/a |
| Validate the access token in the new API | authentication, api → api, data-access, design-tokens | Authentication | – | 16 | 100% | 6% |
| Make sure users can't see another customer's invoice | authorization → – | Authorization | Authorization | 0 | 0% | n/a |
| Users type a search term that goes into a SQL query | input-validation, data-access → – | Input validation, Output encoding, Queries | Input validation, Output encoding, Queries | 0 | 0% | n/a |
| A scanner flagged a vulnerable NuGet package | dependencies → – | Vulnerable packages | Vulnerable packages | 0 | 0% | n/a |
| Add a new package feed for an internal library | dependencies → – | Package sources | Package sources | 0 | 0% | n/a |
| Add logging to the payment workflow | observability, secrets → – | Logging, Sensitive data in logs | Logging, Sensitive data in logs | 0 | 0% | n/a |
| Kubernetes needs a liveness probe for the service | observability → – | Health endpoints | Health endpoints | 0 | 0% | n/a |
| Trace a slow request across services | observability, performance → – | Tracing | Tracing | 0 | 0% | n/a |
| Write tests for the discount calculator | testing → – | Unit tests | Unit tests | 0 | 0% | n/a |
| Test the API end to end with its database | testing → api, testing | Integration tests | – | 13 | 100% | 8% |
| Tune the Kubernetes cluster's network policies | – → – | – | – | 0 | n/a | n/a |
| Train a model to forecast next month's sales | – → sass | – | – | 3 | n/a | 0% |
