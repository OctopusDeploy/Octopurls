The Octopus Deploy docs URL redirector.

## Adding a new short URL redirect
Open `source/Octopurls/redirects.json` in your favourite text editor (or just click the `Edit this file` button in Github.

Add a new key value pair to the list of redirects, like so:
```
{
  ...,
  "ShortUrlName": "http://url.to/redirect/to"
}
```

## Testing your changes
Commit your changes to a feature branch and the site will be automatically be deployed to `http://octopurls-test-webapp.azurewebsites.net/`. 
If you've added a new redirect for e.g. `ShortUrlName`, visit `http://octopurls-test-webapp.azurewebsites.net/ShortUrlName` to test that your redirect sends you to the intended destination.

## Push changes to production site
Commit your changes to the `master` branch and the site will be automatically deployed to `http://g.octopushq.com/`.

**You can check on the progress of the deployment in the `#feed-octopurls` slack channel.**
