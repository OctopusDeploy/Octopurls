The Octopus Deploy docs URL redirector.

## Adding a new short URL redirect
Open `src/Octopurls/redirects.json` in your favourite text editor (or just click the `Edit this file` button in Github.

Add a new key value pair to the list of redirects, like so:
```
{
  ...,
  "ShortUrlName": "http://url.to/redirect/to"
}
```

## Testing your changes
Commit your changes to the `dev` branch and the site will be automatically be deployed to `http://octopurls-test-webapp.azurewebsites.net/`.

## Push changes to production site
Commit your changes to the `master` branch and the site will be automatically deployed to `http://g.octopushq.com/`.
