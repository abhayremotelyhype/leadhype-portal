const http = require('http');

const server = http.createServer((req, res) => {
  console.log(`${new Date().toISOString()} - ${req.method} ${req.url}`);
  
  if (req.method === 'POST' && req.url === '/webhook') {
    let body = '';
    
    req.on('data', chunk => {
      body += chunk.toString();
    });
    
    req.on('end', () => {
      console.log('Webhook received:');
      console.log('Headers:', req.headers);
      try {
        const payload = JSON.parse(body);
        console.log('Payload:', JSON.stringify(payload, null, 2));
      } catch (e) {
        console.log('Raw body:', body);
      }
      
      res.writeHead(200, {'Content-Type': 'application/json'});
      res.end(JSON.stringify({status: 'success', message: 'Webhook received'}));
    });
  } else {
    res.writeHead(404, {'Content-Type': 'application/json'});
    res.end(JSON.stringify({error: 'Not found'}));
  }
});

const PORT = 3003;
server.listen(PORT, () => {
  console.log(`Webhook test server running on port ${PORT}`);
  console.log(`Endpoint: http://localhost:${PORT}/webhook`);
});