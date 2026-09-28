---
title: Acme public website
version: 1.0
company: acme
product: acme-website
technology: [website, ui]
area: [branding, content, navigation, accessibility, user-interaction]
---
<!-- PLACEHOLDER standards for the company's public site. The palette (crimson, greys, white) and
     structure were modeled on a real corporate site for the demo; the name stays Acme. -->

## Colors
- MUST load the website theme after the Acme stylesheet: `{{design-system}}/css/website.css`.
- Custom styles MUST use the website tokens (`var(--site-primary)`, `var(--site-ink)`,
  `var(--site-mist)`, …). MUST NOT use literal hex or rgb values.

Why: the public site has its own crimson, grey and white palette; the theme applies it to every
Acme component at once.

### Tokens
| Token | Value | Use for |
|---|---|---|
| `--site-primary` | `#A2224B` | Brand color: hero, links, primary buttons, header rule |
| `--site-ink` | `#1F1F1F` | Body text, footer background |
| `--site-grey` | `#636B75` | Secondary text |
| `--site-silver` | `#A1A0A1` | Dividers, quiet borders |
| `--site-mist` | `#DCE3EB` | Alternate section background |
| `--site-surface` | `#FFFFFF` | Cards, header |
| `--site-canvas` | `#F6F6F5` | Page background |

### Example
```html
<link rel="stylesheet" href="{{design-system}}/css/acme.css">
<link rel="stylesheet" href="{{design-system}}/css/website.css">
```

## Typography
- Headings and body text MUST use the theme's font stacks (`var(--site-font-heading)`,
  `var(--site-font-body)`); the website theme applies them.
- MUST NOT load fonts from third-party font services.

Why: privacy and page speed. The stacks fall back to system fonts when the brand fonts aren't installed.

## Page structure
- Pages MUST have, in this order: a skip link (`skip-link`), a `site-header` with the logo and
  primary navigation, `<main id="main-content">`, one `site-hero` holding the page's only `<h1>`,
  content in `site-section`s, and a `site-footer`.
- Groups of items (services, people, news) SHOULD be `acme-card`s in a `site-grid`.

Why: every page of the site reads the same way, and assistive technology can jump straight to the
content.

### Classes
| Class | Use for |
|---|---|
| `skip-link` | "Skip to content" link, first thing in `<body>` |
| `site-header`, `site-logo`, `site-nav` | Header, logo link, primary navigation |
| `site-hero` | The hero band with the `<h1>` |
| `site-section`, `site-section--mist` | Content sections; `--mist` for an alternate background |
| `site-grid` | A responsive grid of cards |
| `acme-button--inverse` | A button on the crimson hero |
| `site-footer` | Footer with the legal line and links |

### Example
```html
<body class="acme-page">
  <a class="skip-link" href="#main-content">Skip to content</a>
  <header class="site-header">
    <a class="site-logo" href="/">Acme</a>
    <nav class="site-nav" aria-label="Primary"><ul>
      <li><a href="/" aria-current="page">Home</a></li>
      <li><a href="/who-we-are">Who we are</a></li>
      <li><a href="/our-expertise">Our expertise</a></li>
      <li><a href="/careers">Careers</a></li>
      <li><a href="/news">News</a></li>
      <li><a href="/contact">Contact</a></li>
    </ul></nav>
  </header>
  <main id="main-content">
    <section class="site-hero">
      <h1>Energy that keeps up with change</h1>
      <p>One short sentence on what Acme does.</p>
      <a class="acme-button acme-button--inverse" href="/our-expertise">Explore our expertise</a>
    </section>
    <section class="site-section"><div class="site-grid">
      <article class="acme-card"><h2>Who we are</h2><p>…</p><a href="/who-we-are">Learn more</a></article>
    </div></section>
  </main>
  <footer class="site-footer">…</footer>
</body>
```

## Navigation
- The primary navigation MUST contain these items, in this order: Home, Who we are, Our expertise,
  Careers, News, Contact.
- The current page's link MUST carry `aria-current="page"`.

## Calls to action
- Labels MUST start with a verb and SHOULD be three words or fewer ("Learn more", "Meet our leaders",
  "Explore careers").
- MUST NOT use "Click here".
- The main call to action on a page SHOULD be an `acme-button`; on the hero, `acme-button--inverse`.

## Voice
- Copy SHOULD be professional and confident, lead with what Acme does, and keep sentences under 25
  words.
- The company name MUST be written "Acme": not "ACME", and not "Acme Inc." in body copy.
- Subsidiaries SHOULD be named in full on first mention.

## Accessibility
- The skip link MUST be the first focusable element and MUST target `#main-content`.
- Headings MUST NOT skip levels.
- Meaningful images MUST have alt text; decorative images MUST have `alt=""`.
- MUST NOT remove focus outlines; the Acme and website classes provide visible focus styles.

## Footer
- MUST include the line "© <current year> Acme. All rights reserved." and links to Privacy policy,
  Sitemap and Contact.
- Social links (LinkedIn, Facebook) MUST have an accessible name, such as
  `aria-label="Acme on LinkedIn"`.
