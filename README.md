
<div align="center">
  <a href="www.paolomancaconsulting.com" id="top">
    <img src="https://github.com/Paolomanca90/DHConsulting/blob/master/DHConsulting/Content/Img/Logo-2.png" alt="Logo" width="200" height="60">
  </a>

  <h3 align="center">PM Consulting</h3>

  <p align="center">
    This project represents not only my capstone of the <a href="https://epicode.com/">Epicode</a> course but at the same time the code of my own consultancy website 💻.
    <br />
    <a href="https://github.com/Paolomanca90/DHConsulting"><strong>Explore the docs »</strong></a>
    .
    <a href="www.paolomancaconsulting.com">View Demo</a>
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Contents 📑</summary>
  <ol>
    <li>
      <a href="#about">About The Project</a>
      <ul>
        <li><a href="#built">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contact">Contact</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project 💼

<img src="https://github.com/Paolomanca90/DHConsulting/blob/master/DHConsulting/Content/Img/Logo-2.png" alt="Logo" width="150" height="40" id="about">

Completing a Fullstack programming course is certainly an emotion and doing it by creating something of your own is even more so. This is why although there are many sites and templates dedicated to professionals, freelancers and consultants online, I decided to get involved and show all the skills acquired, in order to provide future users of the site with all the resources I can put into at their disposal to help them develop their ideas and projects.

Obviously you can evaluate the code to get an overview of how I approach programming and how I use various frameworks, graphics libraries and external supports to take maximum advantage and obtain the best possible result.

<p align="right"><a href="#top">🔼</a></p>



### Built With 🧱 <span id="built"></span>

Everything you will see was developed by fully exploiting the power of ASP.NET and its integrations. Below I leave you the complete list:

<ul>
  <li>
    <a href="https://dotnet.microsoft.com/pt-br/learn/aspnet/what-is-aspnet">
    <img src="https://www.easyask.com/wp-content/uploads/2019/02/asp.net-logo-MSA-Technosoft.png" alt="asp.net"              width="100" height="40">
  </a>
  </li>
  <li>
    <a href="https://www.microsoft.com/en-us/sql-server/sql-server-downloads">
    <img src="https://www.cbssolutions.co.uk/wp-content/uploads/2016/07/1768.sql_logo.png" alt="sql"              width="100" height="40">
  </a>
  </li>
  <li>
    <a href="https://www.javascript.com/">
    <img src="https://logowik.com/content/uploads/images/3799-javascript.jpg" alt="js" width="60" height="40">
  </a>
  </li>
  <br>
  <li>
    <a href="https://tailwindcss.com/">
    <img src="https://upload.wikimedia.org/wikipedia/commons/thumb/9/95/Tailwind_CSS_logo.svg/1280px-Tailwind_CSS_logo.svg.png" alt="tailwind"              width="100" height="20">
  </a>
  </li>
</ul>

Also present in this project ChartJs, AlpineJs, AOS.

<p align="right"><a href="#top">🔼</a></p>



<!-- GETTING STARTED -->
## Getting Started



### Prerequisites 1️⃣

This is the base you need to use the software.
* npm
  ```sh
  npm install npm@latest -g
  ```

### Installation 2️⃣

Testing the project is really simple

1. Clone the repo
   ```sh
   git clone https://github.com/your_username_/Project-Name.git
   ```
2. Install NPM packages
   ```sh
   npm install
   ```
3. Create your sandbox credentials for <a href="https://developer.paypal.com/home">PayPal</a> , <a href="https://console.cloud.google.com/">Google+<a/> and you favorite mail client

4. Replace your credentials in `web.config`

5. Generate your DB
   ```sh
   update-database
   ```

You'll find also a copy of my Db in main root if you want to start with some products or users.

<p align="right"><a href="#top">🔼</a></p>



<!-- USAGE EXAMPLES -->
## Usage 3️⃣

The site is divided into 2 sections: one dedicated to admins and one to users with multi-language system (IT-EN).
First of all you will have to create an admin in your DB. This way you will be able to log in and add the consulting products. At the moment they are divided into price ranges €199, €299, €800; obviously you can create others according to your needs.
Perfect! once the products have been created you will be able to register as a user and proceed with the purchase of one of them using your PayPal sandbox credentials.
With registration you will receive an email to confirm your account containing a personal jwt token. Without account confirmation you will not be able to complete the purchase.
You can also sign up simply using your Google account. In this case, after registration you will not be redirected to the Home page but to your Profile page in order to complete the missing fields. Even in this case, without filling in all the fields you will not be able to finalize your purchase as the add to cart and proceed buttons will be disabled.
You will also be able to recover your password if you forget it, but be careful because you only have 3 attempts to enter the correct one, after which you will be blocked for 15 minutes.

<p align="right"><a href="#top">🔼</a></p>



<!-- ROADMAP -->
## Roadmap

- [x] Add security token 🔐
- [x] Add password management 🔑
- [x] Add mail service 📧
- [x] Add Google login 🌐
- [x] Add PayPal payment 💳
- [x] Multi-language Support
    - [x] Italian 🍕
    - [x] English 🍵

I am open to feedback and suggestions to improve the site and especially the user experience

<p align="right"><a href="#top">🔼</a></p>



<!-- LICENSE -->
## License

Domain registered at Aruba.it ©

<p align="right"><a href="#top">🔼</a></p>



<!-- CONTACT -->
## Contact

Paolo Leucio Manca - 3880416518 ☎ - <a href="mailto:info@paolomancaconsulting.com">info@paolomancaconsulting.com</a> 📪

Linkedin: <a href="https://www.linkedin.com/in/paolo-manca-developer/">Paolo Manca</a>

<p align="right"><a href="#top">🔼</a></p>

