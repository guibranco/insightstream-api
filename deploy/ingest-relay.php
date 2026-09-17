<?php
/**
 * InsightStream ingest relay.
 *
 * Configure cPanel to pipe incoming mail for the destination address to this script
 * (cPanel -> Email -> Forwarders/Track Delivery -> "Pipe to a Program", or a .qmail/
 * procmail rule pointing at `|/usr/bin/php /path/to/ingest-relay.php`). The MTA feeds
 * the raw RFC 822 message on STDIN; this script simply relays it to the ingest API.
 *
 * Configure INSIGHTSTREAM_URL and INSIGHTSTREAM_TOKEN via environment variables set in
 * cPanel's pipe configuration, or hardcode them below for a single-purpose mailbox.
 */

$url = getenv('INSIGHTSTREAM_URL') ?: 'https://api.example.com/api/ingest/email';
$token = getenv('INSIGHTSTREAM_TOKEN') ?: '';

$rawEmail = stream_get_contents(STDIN);

if ($rawEmail === false || strlen($rawEmail) === 0) {
    fwrite(STDERR, "insightstream relay: empty message on stdin\n");
    exit(1);
}

$ch = curl_init($url);
curl_setopt_array($ch, [
    CURLOPT_POST => true,
    CURLOPT_POSTFIELDS => $rawEmail,
    CURLOPT_HTTPHEADER => [
        'Content-Type: message/rfc822',
        'X-Ingest-Token: ' . $token,
    ],
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_TIMEOUT => 30,
]);

$response = curl_exec($ch);
$statusCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
$error = curl_error($ch);
curl_close($ch);

if ($response === false) {
    fwrite(STDERR, "insightstream relay: request failed: $error\n");
    exit(1);
}

if ($statusCode !== 200 && $statusCode !== 202) {
    fwrite(STDERR, "insightstream relay: unexpected status $statusCode: $response\n");
    exit(1);
}

// Exit 0 so the MTA does not bounce or retry the mail.
exit(0);
